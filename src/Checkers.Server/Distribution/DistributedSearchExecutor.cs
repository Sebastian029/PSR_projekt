using System.Collections.Concurrent;
using Checkers.Core.Ai;
using Checkers.Server.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Checkers.Server.Distribution;

public sealed class DistributedSearchExecutor : ISearchExecutor
{
    private readonly WorkerPool _pool;
    private readonly ISearchExecutor _fallback;
    private readonly ServerOptions _options;
    private readonly ILogger<DistributedSearchExecutor> _logger;
    private long _remoteJobs;
    private long _fallbackJobs;

    public DistributedSearchExecutor(
        WorkerPool pool,
        ISearchExecutor fallback,
        IOptions<ServerOptions> options,
        ILogger<DistributedSearchExecutor> logger)
    {
        _pool = pool;
        _fallback = fallback;
        _options = options.Value;
        _logger = logger;
    }

    public long RemoteJobs => Interlocked.Read(ref _remoteJobs);

    public long FallbackJobs => Interlocked.Read(ref _fallbackJobs);

    public async Task<SearchResult[]> ExecuteAsync(
        IReadOnlyList<SearchJob> jobs, CancellationToken cancellationToken = default)
    {
        var results = new SearchResult?[jobs.Count];
        var queue = new ConcurrentQueue<int>(Enumerable.Range(0, jobs.Count));
        var workers = _pool.AvailableWorkers();

        if (workers.Count > 0)
        {
            var loops = workers
                .SelectMany(worker => Enumerable.Range(0, Math.Max(1, _options.ConcurrencyPerWorker))
                    .Select(_ => Task.Run(
                        () => RunWorkerLoop(worker, jobs, queue, results, cancellationToken), cancellationToken)))
                .ToList();
            await Task.WhenAll(loops);
        }

        var missing = Enumerable.Range(0, jobs.Count).Where(i => results[i] is null).ToList();
        if (missing.Count > 0)
        {
            _logger.LogWarning(
                "{Missing}/{Total} jobs computed locally (workers available: {Available}/{Configured})",
                missing.Count, jobs.Count, workers.Count, _pool.Workers.Count);

            var local = await _fallback.ExecuteAsync(missing.Select(i => jobs[i]).ToList(), cancellationToken);
            for (var k = 0; k < missing.Count; k++)
            {
                results[missing[k]] = local[k];
            }

            Interlocked.Add(ref _fallbackJobs, missing.Count);
        }

        return results.Select(result => result!.Value).ToArray();
    }

    private async Task RunWorkerLoop(
        WorkerClient worker,
        IReadOnlyList<SearchJob> jobs,
        ConcurrentQueue<int> queue,
        SearchResult?[] results,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.WorkerTimeoutSeconds);
        while (worker.IsAvailable && queue.TryDequeue(out var index))
        {
            try
            {
                var taskId = Guid.NewGuid().ToString("N")[..8];
                results[index] = await worker.SearchAsync(jobs[index], taskId, timeout, cancellationToken);
                Interlocked.Increment(ref _remoteJobs);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested
                                       && ex is RpcException or HttpRequestException or IOException)
            {
                _logger.LogWarning(
                    "Worker {Worker} failed ({Error}); parked for {Cooldown}s, job re-queued",
                    worker.Address,
                    ex is RpcException rpc ? rpc.StatusCode.ToString() : ex.GetType().Name,
                    _options.WorkerCooldownSeconds);
                worker.MarkFailed(TimeSpan.FromSeconds(_options.WorkerCooldownSeconds));
                queue.Enqueue(index);
                return;
            }
        }
    }
}
