using System.Diagnostics;
using Checkers.Core.Ai;
using Checkers.Core.Model;
using Checkers.Core.Rules;
using Checkers.Server.Configuration;
using Checkers.Server.Distribution;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// Usage:
//   dotnet run -c Release --project tools/Checkers.Benchmark -- --depth 7 --granulation 2 --positions 5 \
//       --workers http://127.0.0.1:5001,http://127.0.0.1:5002
// Without --workers only the local baseline is measured.

var depth = 7;
var granulation = 2;
var positions = 5;
var concurrency = 4;
var workers = new List<string>();

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--depth": depth = int.Parse(args[++i]); break;
        case "--granulation": granulation = int.Parse(args[++i]); break;
        case "--positions": positions = int.Parse(args[++i]); break;
        case "--concurrency": concurrency = int.Parse(args[++i]); break;
        case "--workers":
            workers.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            break;
        default:
            Console.Error.WriteLine($"Unknown argument {args[i]}");
            return 2;
    }
}

var local = new LocalSearchExecutor();
ISearchExecutor? distributed = null;
WorkerPool? pool = null;
if (workers.Count > 0)
{
    var options = Options.Create(new ServerOptions { Workers = workers, ConcurrencyPerWorker = concurrency });
    pool = new WorkerPool(options, new WorkerChannelFactory());
    distributed = new DistributedSearchExecutor(pool, local, options, NullLogger<DistributedSearchExecutor>.Instance);
}

Console.WriteLine($"depth {depth}, granulation {granulation}, {positions} positions, {Environment.ProcessorCount} local cores, {workers.Count} worker(s)");
Console.WriteLine();
Console.WriteLine($"{"position",-10}{"jobs",8}{"local ms",12}{"workers ms",14}{"speedup",10}  same move");

var localTotal = 0.0;
var remoteTotal = 0.0;
foreach (var (board, side, label) in BenchmarkPositions(positions))
{
    var (localResult, localMs) = await Measure(local);
    localTotal += localMs;

    if (distributed is null)
    {
        Console.WriteLine($"{label,-10}{localResult.Jobs,8}{localMs,12:F0}{"-",14}{"-",10}");
        continue;
    }

    var (remoteResult, remoteMs) = await Measure(distributed);
    remoteTotal += remoteMs;
    var same = localResult.Move.Equals(remoteResult.Move) && localResult.Score == remoteResult.Score;
    Console.WriteLine(
        $"{label,-10}{localResult.Jobs,8}{localMs,12:F0}{remoteMs,14:F0}{localMs / remoteMs,9:F2}x  {(same ? "yes" : "NO")}");

    async Task<(BestMoveResult Result, double Ms)> Measure(ISearchExecutor executor)
    {
        var player = new AiPlayer(executor);
        var started = Stopwatch.GetTimestamp();
        var result = await player.FindBestMoveAsync(board, side, new SearchOptions(depth, granulation))
                     ?? throw new InvalidOperationException("No move found.");
        return (result, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}

Console.WriteLine();
Console.WriteLine($"total local: {localTotal:F0} ms");
if (distributed is not null)
{
    Console.WriteLine($"total workers: {remoteTotal:F0} ms (overall speedup {localTotal / remoteTotal:F2}x)");
    foreach (var worker in pool!.Workers)
    {
        Console.WriteLine($"  {worker.Address}: {worker.CompletedJobs} jobs, {worker.FailedJobs} failures");
    }
}

pool?.Dispose();
return 0;

static IEnumerable<(Board Board, Side Side, string Label)> BenchmarkPositions(int count)
{
    // Deterministic mid-game positions reached by seeded random play from the opening.
    var produced = 0;
    for (var seed = 1; produced < count && seed < 1000; seed++)
    {
        var random = new Random(seed);
        var board = Board.CreateInitial();
        var side = Side.White;
        for (var ply = 0; ply < 10; ply++)
        {
            var moves = MoveGenerator.Generate(board, side);
            if (moves.Count == 0)
            {
                break;
            }

            board = MoveGenerator.Apply(board, moves[random.Next(moves.Count)]);
            side = side.Opposite();
        }

        if (MoveGenerator.Generate(board, side).Count >= 4)
        {
            produced++;
            yield return (board, side, $"#{produced}");
        }
    }
}
