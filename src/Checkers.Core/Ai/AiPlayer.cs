using System.Diagnostics;
using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Ai;

public sealed record SearchOptions(int Depth, int Granulation = 1)
{
    public const int MaxDepth = 12;

    public void Validate()
    {
        if (Depth is < 1 or > MaxDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(Depth), Depth, $"Depth must be between 1 and {MaxDepth}.");
        }

        if (Granulation < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Granulation), Granulation, "Granulation must be at least 1.");
        }
    }
}

public sealed record BestMoveResult(Move Move, int Score, long Nodes, int Jobs, TimeSpan Elapsed, bool WasForced);

public sealed class AiPlayer
{
    private readonly ISearchExecutor _executor;
    private readonly Random? _tieBreaker;

    public AiPlayer(ISearchExecutor executor, Random? tieBreaker = null)
    {
        _executor = executor;
        _tieBreaker = tieBreaker;
    }

    public async Task<BestMoveResult?> FindBestMoveAsync(
        Board board,
        Side side,
        SearchOptions options,
        Square? mustContinueFrom = null,
        CancellationToken cancellationToken = default)
    {
        options.Validate();
        var started = Stopwatch.GetTimestamp();

        var rootMoves = MoveGenerator.Generate(board, side, mustContinueFrom);
        if (rootMoves.Count == 0)
        {
            return null;
        }

        if (rootMoves.Count == 1)
        {
            return new BestMoveResult(rootMoves[0], 0, 0, 0, Stopwatch.GetElapsedTime(started), WasForced: true);
        }

        var rootNodes = rootMoves
            .Select(move => new Node(MoveGenerator.Apply(board, move), side.Opposite(), options.Depth - 1))
            .ToList();

        var leaves = Expand(rootNodes, Math.Min(options.Granulation, options.Depth) - 1);
        var jobs = leaves.Select(leaf => new SearchJob(leaf.Board, leaf.SideToMove, leaf.RemainingDepth)).ToList();
        var results = await _executor.ExecuteAsync(jobs, cancellationToken);
        if (results.Length != jobs.Count)
        {
            throw new InvalidOperationException(
                $"Executor returned {results.Length} results for {jobs.Count} jobs.");
        }

        long nodes = 0;
        for (var i = 0; i < leaves.Count; i++)
        {
            leaves[i].Score = results[i].Score;
            nodes += results[i].Nodes;
        }

        var scores = rootNodes.Select(node => node.Resolve()).ToList();
        var bestScore = side == Side.White ? scores.Max() : scores.Min();
        var best = Enumerable.Range(0, scores.Count).Where(i => scores[i] == bestScore).ToList();
        var bestIndex = _tieBreaker is null ? best[0] : best[_tieBreaker.Next(best.Count)];

        return new BestMoveResult(
            rootMoves[bestIndex], bestScore, nodes, jobs.Count, Stopwatch.GetElapsedTime(started), WasForced: false);
    }

    private static List<Node> Expand(List<Node> frontier, int extraPlies)
    {
        for (var ply = 0; ply < extraPlies; ply++)
        {
            var next = new List<Node>();
            foreach (var node in frontier)
            {
                var moves = node.RemainingDepth > 0 ? MoveGenerator.Generate(node.Board, node.SideToMove) : [];
                if (moves.Count == 0)
                {
                    next.Add(node);
                    continue;
                }

                node.Children = moves
                    .Select(move => new Node(
                        MoveGenerator.Apply(node.Board, move), node.SideToMove.Opposite(), node.RemainingDepth - 1))
                    .ToList();
                next.AddRange(node.Children);
            }

            frontier = next;
        }

        return frontier;
    }

    private sealed class Node(Board board, Side sideToMove, int remainingDepth)
    {
        public Board Board { get; } = board;

        public Side SideToMove { get; } = sideToMove;

        public int RemainingDepth { get; } = remainingDepth;

        public List<Node>? Children { get; set; }

        public int Score { get; set; }

        public int Resolve()
        {
            if (Children is null)
            {
                return Score;
            }

            return SideToMove == Side.White
                ? Children.Max(child => child.Resolve())
                : Children.Min(child => child.Resolve());
        }
    }
}
