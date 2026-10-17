using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Game;

public enum GameStatus
{
    InProgress,
    WhiteWon,
    BlackWon,
    Draw,
}

/// <param name="Success">Whether the step was legal and has been played.</param>
/// <param name="Error">Why the step was rejected.</param>
/// <param name="TurnEnded">False when the same player must keep capturing with the same piece.</param>
public readonly record struct StepResult(bool Success, string? Error = null, bool TurnEnded = false)
{
    public static StepResult Rejected(string error) => new(false, error);
}

/// <summary>
/// One game of draughts. Moves are played one step at a time (a multi-capture is several steps by the
/// same piece), which is what an interactive client sends; <see cref="PlayMove"/> plays a whole
/// engine-generated <see cref="Move"/> as a series of steps.
/// This class is not thread-safe; callers must serialise access.
/// </summary>
public sealed class CheckersGame
{
    /// <summary>A position that occurs this many times ends the game as a draw.</summary>
    public const int RepetitionLimit = 3;

    /// <summary>Plies without a capture or a man move after which the game is drawn.</summary>
    public const int QuietPlyLimit = 80;

    private readonly Dictionary<string, int> _positionCounts = new();
    private int _quietPlies;

    public CheckersGame() => Reset();

    /// <summary>Starts a game from an arbitrary position (puzzles, tests, analysis).</summary>
    public static CheckersGame FromPosition(Board board, Side sideToMove)
    {
        var game = new CheckersGame();
        game.Initialize(board.Clone(), sideToMove);
        return game;
    }

    public Board Board { get; private set; } = Board.CreateInitial();

    public Side SideToMove { get; private set; } = Side.White;

    /// <summary>Set while the current player is in the middle of a multi-capture.</summary>
    public Square? MustContinueFrom { get; private set; }

    public GameStatus Status { get; private set; }

    public string? DrawReason { get; private set; }

    public bool IsOver => Status != GameStatus.InProgress;

    public int PlyCount { get; private set; }

    public void Reset() => Initialize(Board.CreateInitial(), Side.White);

    private void Initialize(Board board, Side sideToMove)
    {
        Board = board;
        SideToMove = sideToMove;
        MustContinueFrom = null;
        Status = GameStatus.InProgress;
        DrawReason = null;
        PlyCount = 0;
        _quietPlies = 0;
        _positionCounts.Clear();
        _positionCounts[PositionKey()] = 1;

        if (!MoveGenerator.HasMoves(Board, SideToMove))
        {
            Status = SideToMove == Side.White ? GameStatus.BlackWon : GameStatus.WhiteWon;
        }
    }

    /// <summary>The moves currently legal for the side to move (full capture sequences included).</summary>
    public List<Move> GetLegalMoves() =>
        IsOver ? [] : MoveGenerator.Generate(Board, SideToMove, MustContinueFrom);

    public StepResult TryPlayStep(Square from, Square to)
    {
        if (IsOver)
        {
            return StepResult.Rejected("The game is over.");
        }

        if (!from.IsOnBoard || !to.IsOnBoard)
        {
            return StepResult.Rejected("Coordinates are outside the board.");
        }

        var piece = Board.Get(from);
        if (piece.IsEmpty() || !piece.BelongsTo(SideToMove))
        {
            return StepResult.Rejected($"There is no {SideToMove.ToString().ToLowerInvariant()} piece on {from}.");
        }

        if (MustContinueFrom is { } mustContinue && mustContinue != from)
        {
            return StepResult.Rejected($"The capture must be continued with the piece on {mustContinue}.");
        }

        var anyCapture = MustContinueFrom is not null
            || Board.PiecesOf(SideToMove).Any(square => MoveGenerator.GetCaptureSteps(Board, square).Count > 0);

        if (anyCapture)
        {
            var step = MoveGenerator.GetCaptureSteps(Board, from).FirstOrDefault(s => s.To == to);
            if (step == default)
            {
                return StepResult.Rejected("A capture is mandatory.");
            }

            return PlayCapture(from, piece, step);
        }

        var quiet = MoveGenerator.GenerateQuietMoves(Board, SideToMove)
            .FirstOrDefault(move => move.From == from && move.To == to);
        if (quiet is null)
        {
            return StepResult.Rejected("Illegal move.");
        }

        Board = MoveGenerator.Apply(Board, quiet);
        EndTurn(progress: piece.IsMan());
        return new StepResult(true, TurnEnded: true);
    }

    /// <summary>Plays a complete move produced by the move generator.</summary>
    public void PlayMove(Move move)
    {
        var from = move.From;
        foreach (var landing in move.Path)
        {
            var result = TryPlayStep(from, landing);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Move {move} is not legal here: {result.Error}");
            }

            from = landing;
        }
    }

    private StepResult PlayCapture(Square from, Piece piece, CaptureStep step)
    {
        // Boards handed out earlier must stay valid snapshots, so never mutate the current one in place.
        var next = Board.Clone();
        next.Set(from, Piece.Empty);
        next.Set(step.Captured, Piece.Empty);

        var promotes = MoveGenerator.Promotes(piece, step.To);
        next.Set(step.To, promotes ? piece.Promoted() : piece);
        Board = next;

        if (!promotes && MoveGenerator.GetCaptureSteps(Board, step.To).Count > 0)
        {
            MustContinueFrom = step.To;
            return new StepResult(true, TurnEnded: false);
        }

        EndTurn(progress: true);
        return new StepResult(true, TurnEnded: true);
    }

    private void EndTurn(bool progress)
    {
        MustContinueFrom = null;
        SideToMove = SideToMove.Opposite();
        PlyCount++;

        if (progress)
        {
            // Captures and man moves are irreversible, so earlier positions can never recur.
            _quietPlies = 0;
            _positionCounts.Clear();
        }
        else
        {
            _quietPlies++;
        }

        var key = PositionKey();
        var occurrences = _positionCounts.GetValueOrDefault(key) + 1;
        _positionCounts[key] = occurrences;

        if (!MoveGenerator.HasMoves(Board, SideToMove))
        {
            Status = SideToMove == Side.White ? GameStatus.BlackWon : GameStatus.WhiteWon;
        }
        else if (occurrences >= RepetitionLimit)
        {
            Status = GameStatus.Draw;
            DrawReason = "Threefold repetition";
        }
        else if (_quietPlies >= QuietPlyLimit)
        {
            Status = GameStatus.Draw;
            DrawReason = $"{QuietPlyLimit} plies without a capture or a man move";
        }
    }

    private string PositionKey() => $"{(SideToMove == Side.White ? 'w' : 'b')}{Board.ToKey()}";
}
