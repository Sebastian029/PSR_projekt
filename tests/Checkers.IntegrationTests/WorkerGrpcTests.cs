using Checkers.Contracts;
using Checkers.Core.Ai;
using Checkers.Core.Model;
using Google.Protobuf;
using Grpc.Core;

namespace Checkers.IntegrationTests;

public sealed class WorkerGrpcTests : IDisposable
{
    private readonly InMemoryWorker _worker = new("worker-under-test");
    private readonly SearchService.SearchServiceClient _client;

    public WorkerGrpcTests() => _client = new SearchService.SearchServiceClient(_worker.CreateChannel());

    public void Dispose() => _worker.Dispose();

    [Fact]
    public async Task PingDescribesTheWorker()
    {
        var response = await _client.PingAsync(new PingRequest());

        Assert.Equal("worker-under-test", response.Worker);
        Assert.True(response.Cores >= 1);
    }

    [Fact]
    public async Task SearchReturnsTheSameScoreAsALocalSearch()
    {
        foreach (var job in Positions.RandomJobs(6, depth: 4))
        {
            var response = await _client.SearchAsync(ProtoMapping.ToRequest(job, "t"));

            var expected = MinimaxSearcher.Search(job.Board, job.SideToMove, job.Depth);
            Assert.Equal(expected.Score, response.Score);
            Assert.Equal(expected.Nodes, response.Nodes);
            Assert.Equal("worker-under-test", response.Worker);
        }
    }

    [Fact]
    public async Task SearchSeesTheWholeBoardIncludingTheLastRows()
    {
        // Regression: the legacy board packing lost rows 6-7. A lone black king on row 7 next to a
        // white man that can be captured must change the result compared with the same board without it.
        var withPiece = Board.Parse(
            "........", "........", "........", "........", "........", "..w.....", "........", "B.......");
        var without = Board.Parse(
            "........", "........", "........", "........", "........", "..w.....", "........", "........");

        var a = await _client.SearchAsync(ProtoMapping.ToRequest(new SearchJob(withPiece, Side.Black, 2), "a"));
        var b = await _client.SearchAsync(ProtoMapping.ToRequest(new SearchJob(without, Side.Black, 2), "b"));

        Assert.NotEqual(a.Score, b.Score);
        Assert.Equal(MinimaxSearcher.Search(withPiece, Side.Black, 2).Score, a.Score);
    }

    [Theory]
    [InlineData(24, 2)]
    [InlineData(32, 99)]
    [InlineData(32, -1)]
    public async Task MalformedRequestsAreRejectedAsInvalidArgument(int boardBytes, int depth)
    {
        var request = new SearchRequest
        {
            Board = ByteString.CopyFrom(new byte[boardBytes]),
            WhiteToMove = true,
            Depth = depth,
        };

        var error = await Assert.ThrowsAsync<RpcException>(async () => await _client.SearchAsync(request));

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Fact]
    public async Task CallerCancellationIsReportedAsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var job = new SearchJob(Board.CreateInitial(), Side.White, 4);

        var error = await Assert.ThrowsAnyAsync<Exception>(
            async () => await _client.SearchAsync(ProtoMapping.ToRequest(job, "c"), cancellationToken: cts.Token));

        Assert.True(error is RpcException { StatusCode: StatusCode.Cancelled } or OperationCanceledException);
    }
}
