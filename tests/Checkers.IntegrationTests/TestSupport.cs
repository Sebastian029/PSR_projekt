using Checkers.Core.Ai;
using Checkers.Core.Model;
using Checkers.Core.Rules;
using Checkers.Server.Distribution;
using Checkers.Worker;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Checkers.IntegrationTests;

internal sealed class InMemoryWorker : IDisposable
{
    private readonly WebApplicationFactory<SearchServiceImpl> _factory;

    public InMemoryWorker(string name)
    {
        _factory = new WebApplicationFactory<SearchServiceImpl>().WithWebHostBuilder(builder =>
            builder.UseSetting("Worker:Name", name).UseSetting("Worker:MaxConcurrentSearches", "2"));
    }

    public GrpcChannel CreateChannel() => GrpcChannel.ForAddress(
        _factory.Server.BaseAddress,
        new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });

    public void Dispose() => _factory.Dispose();
}

internal sealed class FakeChannelFactory : IWorkerChannelFactory
{
    private readonly Dictionary<string, InMemoryWorker> _workers = new(StringComparer.OrdinalIgnoreCase);

    public FakeChannelFactory Add(string address, InMemoryWorker worker)
    {
        _workers[address] = worker;
        return this;
    }

    public GrpcChannel Create(string address) =>
        _workers.TryGetValue(address, out var worker) ? worker.CreateChannel() : GrpcChannel.ForAddress(address);
}

internal static class Positions
{
    public static List<SearchJob> RandomJobs(int count, int depth)
    {
        var jobs = new List<SearchJob>();
        for (var i = 0; i < count; i++)
        {
            var random = new Random(500 + i);
            var board = Board.CreateInitial();
            var side = Side.White;
            for (var ply = 0; ply < 4 + i % 9; ply++)
            {
                var moves = MoveGenerator.Generate(board, side);
                if (moves.Count == 0)
                {
                    break;
                }

                board = MoveGenerator.Apply(board, moves[random.Next(moves.Count)]);
                side = side.Opposite();
            }

            jobs.Add(new SearchJob(board, side, depth));
        }

        return jobs;
    }

    public static int[] ExpectedScores(IReadOnlyList<SearchJob> jobs) =>
        jobs.Select(job => MinimaxSearcher.Search(job.Board, job.SideToMove, job.Depth).Score).ToArray();
}
