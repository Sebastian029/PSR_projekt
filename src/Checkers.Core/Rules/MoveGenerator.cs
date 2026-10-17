using Checkers.Core.Model;

namespace Checkers.Core.Rules;

/// <summary>A single capturing jump: the landing square and the piece that is removed.</summary>
public readonly record struct CaptureStep(Square To, Square Captured);

/// <summary>
/// Rules of the variant implemented here:
/// 8x8 board, white moves first; men move and capture forward only; kings move and capture along
/// any distance ("flying kings"); capturing is mandatory (any capture, no majority rule);
/// a capturing piece keeps jumping until it cannot; a man that reaches the last row is promoted
/// and its turn ends. A player without a legal move loses.
/// </summary>
public static class MoveGenerator
{
    private static readonly (int Row, int Col)[] Diagonals = [(-1, -1), (-1, 1), (1, -1), (1, 1)];

    private static readonly int[] ColumnDeltas = [-1, 1];

    /// <summary>
    /// All legal moves for <paramref name="side"/>. When <paramref name="mustContinueFrom"/> is set the
    /// player is in the middle of a capture sequence and only that piece may move, and only by capturing.
    /// </summary>
    public static List<Move> Generate(Board board, Side side, Square? mustContinueFrom = null)
    {
        var captures = GenerateCaptures(board, side, mustContinueFrom);
        if (captures.Count > 0 || mustContinueFrom is not null)
        {
            return captures;
        }

        return GenerateQuietMoves(board, side);
    }

    public static bool HasMoves(Board board, Side side)
    {
        foreach (var square in board.PiecesOf(side))
        {
            if (GetCaptureSteps(board, square).Count > 0 || HasQuietMove(board, square))
            {
                return true;
            }
        }

        return false;
    }

    public static List<Move> GenerateCaptures(Board board, Side side, Square? onlyFrom = null)
    {
        var moves = new List<Move>();
        var scratch = board.Clone();
        IEnumerable<Square> origins = onlyFrom is { } only
            ? (board.Get(only).BelongsTo(side) ? [only] : [])
            : board.PiecesOf(side);

        foreach (var origin in origins)
        {
            ExtendCapture(scratch, scratch.Get(origin), origin, origin, [], [], moves);
        }

        return moves;
    }

    public static List<Move> GenerateQuietMoves(Board board, Side side)
    {
        var moves = new List<Move>();
        foreach (var from in board.PiecesOf(side))
        {
            if (board.Get(from).IsKing())
            {
                foreach (var (dRow, dCol) in Diagonals)
                {
                    var to = from.Offset(dRow, dCol);
                    while (to.IsOnBoard && board.Get(to).IsEmpty())
                    {
                        moves.Add(new Move(from, [to], []));
                        to = to.Offset(dRow, dCol);
                    }
                }
            }
            else
            {
                var dRow = side.ForwardRowDelta();
                foreach (var dCol in ColumnDeltas)
                {
                    var to = from.Offset(dRow, dCol);
                    if (to.IsOnBoard && board.Get(to).IsEmpty())
                    {
                        moves.Add(new Move(from, [to], []));
                    }
                }
            }
        }

        return moves;
    }

    /// <summary>Single capturing jumps available to the piece standing on <paramref name="from"/>.</summary>
    public static List<CaptureStep> GetCaptureSteps(Board board, Square from)
    {
        var steps = new List<CaptureStep>();
        var piece = board.Get(from);
        if (piece.IsEmpty())
        {
            return steps;
        }

        var side = piece.SideOf();
        if (piece.IsKing())
        {
            foreach (var (dRow, dCol) in Diagonals)
            {
                var victim = from.Offset(dRow, dCol);
                while (victim.IsOnBoard && board.Get(victim).IsEmpty())
                {
                    victim = victim.Offset(dRow, dCol);
                }

                if (!victim.IsOnBoard || board.Get(victim).BelongsTo(side))
                {
                    continue;
                }

                var landing = victim.Offset(dRow, dCol);
                while (landing.IsOnBoard && board.Get(landing).IsEmpty())
                {
                    steps.Add(new CaptureStep(landing, victim));
                    landing = landing.Offset(dRow, dCol);
                }
            }
        }
        else
        {
            var dRow = side.ForwardRowDelta();
            foreach (var dCol in ColumnDeltas)
            {
                var victim = from.Offset(dRow, dCol);
                var landing = from.Offset(2 * dRow, 2 * dCol);
                var victimPiece = board.Get(victim);
                if (landing.IsOnBoard && !victimPiece.IsEmpty() && !victimPiece.BelongsTo(side)
                    && board.Get(landing).IsEmpty())
                {
                    steps.Add(new CaptureStep(landing, victim));
                }
            }
        }

        return steps;
    }

    /// <summary>True when the piece becomes a king by standing on <paramref name="square"/>.</summary>
    public static bool Promotes(Piece piece, Square square) =>
        piece.IsMan() && square.Row == piece.SideOf().PromotionRow();

    /// <summary>Returns a new board with the whole move applied (captures removed, promotion done).</summary>
    public static Board Apply(Board board, Move move)
    {
        var result = board.Clone();
        var piece = result.Get(move.From);
        result.Set(move.From, Piece.Empty);
        foreach (var captured in move.Captured)
        {
            result.Set(captured, Piece.Empty);
        }

        result.Set(move.To, Promotes(piece, move.To) ? piece.Promoted() : piece);
        return result;
    }

    private static bool HasQuietMove(Board board, Square from)
    {
        var piece = board.Get(from);
        if (piece.IsKing())
        {
            foreach (var (dRow, dCol) in Diagonals)
            {
                var to = from.Offset(dRow, dCol);
                if (to.IsOnBoard && board.Get(to).IsEmpty())
                {
                    return true;
                }
            }

            return false;
        }

        var forward = piece.SideOf().ForwardRowDelta();
        foreach (var dCol in ColumnDeltas)
        {
            var to = from.Offset(forward, dCol);
            if (to.IsOnBoard && board.Get(to).IsEmpty())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Depth-first enumeration of capture sequences. The scratch board is mutated and restored, so the
    /// captured pieces vanish immediately and cannot be jumped twice.
    /// </summary>
    private static void ExtendCapture(
        Board scratch, Piece piece, Square origin, Square current, List<Square> path, List<Square> captured, List<Move> output)
    {
        var steps = GetCaptureSteps(scratch, current);
        if (steps.Count == 0)
        {
            if (captured.Count > 0)
            {
                output.Add(new Move(origin, path.ToArray(), captured.ToArray()));
            }

            return;
        }

        foreach (var step in steps)
        {
            var victim = scratch.Get(step.Captured);
            var promotes = Promotes(piece, step.To);
            var landed = promotes ? piece.Promoted() : piece;

            scratch.Set(current, Piece.Empty);
            scratch.Set(step.Captured, Piece.Empty);
            scratch.Set(step.To, landed);
            path.Add(step.To);
            captured.Add(step.Captured);

            if (promotes)
            {
                output.Add(new Move(origin, path.ToArray(), captured.ToArray()));
            }
            else
            {
                ExtendCapture(scratch, landed, origin, step.To, path, captured, output);
            }

            captured.RemoveAt(captured.Count - 1);
            path.RemoveAt(path.Count - 1);
            scratch.Set(step.To, Piece.Empty);
            scratch.Set(step.Captured, victim);
            scratch.Set(current, piece);
        }
    }
}
