using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Ai;

public readonly record struct SearchResult(int Score, long Nodes, long ComputeMs);

public sealed record SearchJob(Board Board, Side SideToMove, int Depth);

public static class MinimaxSearcher
{
    public const int WinScore = 100_000;

    private const int Infinity = int.MaxValue;

    public static SearchResult Search(Board board, Side sideToMove, int depth, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        long nodes = 0;
        var score = AlphaBeta(board, sideToMove, depth, -Infinity, Infinity, ref nodes, cancellationToken);
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);
        return new SearchResult(score, nodes, (long)elapsed.TotalMilliseconds);
    }

    internal static int PlainMinimax(Board board, Side sideToMove, int depth)
    {
        if (!MoveGenerator.HasMoves(board, sideToMove))
        {
            return LossScore(sideToMove, depth);
        }

        if (depth == 0)
        {
            return Evaluator.Evaluate(board);
        }

        var best = sideToMove == Side.White ? -Infinity : Infinity;
        foreach (var move in MoveGenerator.Generate(board, sideToMove))
        {
            var score = PlainMinimax(MoveGenerator.Apply(board, move), sideToMove.Opposite(), depth - 1);
            best = sideToMove == Side.White ? Math.Max(best, score) : Math.Min(best, score);
        }

        return best;
    }

    private static int AlphaBeta(
        Board board, Side side, int depth, int alpha, int beta, ref long nodes, CancellationToken cancellationToken)
    {
        nodes++;
        if ((nodes & 0x3FF) == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (depth == 0)
        {
            return MoveGenerator.HasMoves(board, side) ? Evaluator.Evaluate(board) : LossScore(side, depth);
        }

        var moves = MoveGenerator.Generate(board, side);
        if (moves.Count == 0)
        {
            return LossScore(side, depth);
        }

        if (side == Side.White)
        {
            var best = -Infinity;
            foreach (var move in moves)
            {
                var score = AlphaBeta(
                    MoveGenerator.Apply(board, move), Side.Black, depth - 1, alpha, beta, ref nodes, cancellationToken);
                best = Math.Max(best, score);
                alpha = Math.Max(alpha, best);
                if (alpha >= beta)
                {
                    break;
                }
            }

            return best;
        }
        else
        {
            var best = Infinity;
            foreach (var move in moves)
            {
                var score = AlphaBeta(
                    MoveGenerator.Apply(board, move), Side.White, depth - 1, alpha, beta, ref nodes, cancellationToken);
                best = Math.Min(best, score);
                beta = Math.Min(beta, best);
                if (alpha >= beta)
                {
                    break;
                }
            }

            return best;
        }
    }

    private static int LossScore(Side sideToMove, int remainingDepth) =>
        sideToMove == Side.White ? -(WinScore + remainingDepth) : WinScore + remainingDepth;
}
