using Checkers.Contracts;
using Checkers.Core.Ai;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Checkers.Worker;

public sealed class SearchServiceImpl(
    ILogger<SearchServiceImpl> logger,
    IOptions<WorkerOptions> options,
    SearchGate gate) : SearchService.SearchServiceBase
{
    private readonly WorkerOptions _options = options.Value;

    public override async Task<SearchResponse> Search(SearchRequest request, ServerCallContext context)
    {
        SearchJob job;
        try
        {
            job = ProtoMapping.ToJob(request);
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }

        var cancellation = context.CancellationToken;
        try
        {
            using var _ = await gate.EnterAsync(cancellation);
            var result = await Task.Run(
                () => MinimaxSearcher.Search(job.Board, job.SideToMove, job.Depth, cancellation), cancellation);

            logger.LogDebug(
                "Task {TaskId}: depth {Depth} -> score {Score}, {Nodes} nodes in {Ms} ms",
                request.TaskId, job.Depth, result.Score, result.Nodes, result.ComputeMs);

            return new SearchResponse
            {
                Score = result.Score,
                Nodes = result.Nodes,
                ComputeMs = result.ComputeMs,
                Worker = _options.ResolvedName,
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Search cancelled by the caller."));
        }
    }

    public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context) =>
        Task.FromResult(new PingResponse
        {
            Worker = _options.ResolvedName,
            Cores = Environment.ProcessorCount,
            Version = typeof(SearchServiceImpl).Assembly.GetName().Version?.ToString() ?? "unknown",
        });
}
