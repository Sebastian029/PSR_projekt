using Checkers.Core.Ai;
using Checkers.Core.Model;
using Checkers.Server.Configuration;
using Checkers.Server.Distribution;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Checkers.IntegrationTests;

public sealed class DistributedSearchExecutorTests : IDisposable
{
    private const string Dead = "http://127.0.0.1:1";

    private readonly InMemoryWorker _workerA = new("A");
    private readonly InMemoryWorker _workerB = new("B");
    private readonly List<WorkerPool> _pools = [];

    public void Dispose()
    {
        foreach (var pool in _pools)
        {
            pool.Dispose();
        }

        _workerA.Dispose();
        _workerB.Dispose();
    }

    private (DistributedSearchExecutor Executor, WorkerPool Pool) Create(
        FakeChannelFactory channels, string[] addresses, TimeProvider? time = null, int concurrency = 2)
    {
        var options = Options.Create(new ServerOptions
        {
            Workers = [.. addresses],
            ConcurrencyPerWorker = concurrency,
            WorkerTimeoutSeconds = 30,
            WorkerCooldownSeconds = 15,
        });
        var pool = new WorkerPool(options, channels, time);
        _pools.Add(pool);
        var executor = new DistributedSearchExecutor(
            pool, new LocalSearchExecutor(2), options, NullLogger<DistributedSearchExecutor>.Instance);
        return (executor, pool);
    }

    [Fact]
    public async Task ResultsMatchLocalSearchAndAreSharedBetweenWorkers()
    {
        var channels = new FakeChannelFactory().Add("http://a", _workerA).Add("http://b", _workerB);
        var (executor, pool) = Create(channels, ["http://a", "http://b"]);
        var jobs = Positions.RandomJobs(24, depth: 3);

        var results = await executor.ExecuteAsync(jobs);

        Assert.Equal(Positions.ExpectedScores(jobs), results.Select(r => r.Score));
        Assert.All(pool.Workers, worker => Assert.True(worker.CompletedJobs > 0, $"{worker.Address} did no work"));
        Assert.Equal(jobs.Count, executor.RemoteJobs);
        Assert.Equal(0, executor.FallbackJobs);
    }

    [Fact]
    public async Task DeadWorkerIsSkippedAndItsJobsAreServedByTheOthers()
    {
        var channels = new FakeChannelFactory().Add("http://a", _workerA);
        var (executor, pool) = Create(channels, [Dead, "http://a"]);
        var jobs = Positions.RandomJobs(12, depth: 3);

        var results = await executor.ExecuteAsync(jobs);

        Assert.Equal(Positions.ExpectedScores(jobs), results.Select(r => r.Score));
        var dead = pool.Workers.Single(w => w.Address == Dead);
        Assert.False(dead.IsAvailable);
        Assert.True(dead.FailedJobs > 0);
        Assert.Equal(jobs.Count, executor.RemoteJobs + executor.FallbackJobs);
    }

    [Fact]
    public async Task WhenEveryWorkerIsDownTheJobsAreComputedLocally()
    {
        var (executor, _) = Create(new FakeChannelFactory(), [Dead, "http://127.0.0.1:2"]);
        var jobs = Positions.RandomJobs(10, depth: 3);

        var results = await executor.ExecuteAsync(jobs);

        Assert.Equal(Positions.ExpectedScores(jobs), results.Select(r => r.Score));
        Assert.Equal(jobs.Count, executor.FallbackJobs);
        Assert.Equal(0, executor.RemoteJobs);
    }

    [Fact]
    public async Task FailedWorkerIsRetriedAfterTheCooldown()
    {
        var time = new FakeTimeProvider();
        var (executor, pool) = Create(new FakeChannelFactory(), [Dead], time);
        await executor.ExecuteAsync(Positions.RandomJobs(2, depth: 1));
        var worker = pool.Workers.Single();
        Assert.False(worker.IsAvailable);

        time.Advance(TimeSpan.FromSeconds(14));
        Assert.False(worker.IsAvailable);

        time.Advance(TimeSpan.FromSeconds(2));
        Assert.True(worker.IsAvailable);
    }

    [Fact]
    public async Task EmptyJobListReturnsEmptyResult()
    {
        var (executor, _) = Create(new FakeChannelFactory().Add("http://a", _workerA), ["http://a"]);

        var results = await executor.ExecuteAsync([]);

        Assert.Empty(results);
    }

    [Fact]
    public async Task CancellationStopsTheExecution()
    {
        var channels = new FakeChannelFactory().Add("http://a", _workerA);
        var (executor, _) = Create(channels, ["http://a"]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => executor.ExecuteAsync(Positions.RandomJobs(4, depth: 6), cts.Token));
    }

    [Fact]
    public async Task AiPlayerPicksTheSameMoveLocallyAndOverGrpc()
    {
        var channels = new FakeChannelFactory().Add("http://a", _workerA).Add("http://b", _workerB);
        var (executor, _) = Create(channels, ["http://a", "http://b"]);
        var distributed = new AiPlayer(executor);
        var local = new AiPlayer(new LocalSearchExecutor());
        var board = Board.CreateInitial();

        var remote = await distributed.FindBestMoveAsync(board, Side.White, new SearchOptions(5, 2));
        var expected = await local.FindBestMoveAsync(board, Side.White, new SearchOptions(5, 2));

        Assert.Equal(expected!.Move, remote!.Move);
        Assert.Equal(expected.Score, remote.Score);
    }
}
