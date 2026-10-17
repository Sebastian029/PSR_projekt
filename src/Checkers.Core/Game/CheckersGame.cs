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

public readonly record struct StepResult(bool Success, string? Error = null, bool TurnEnded = false)
{
    public static StepResult Rejected(string error) => new(false, error);
}

public sealed class CheckersGame
{
    public const int RepetitionLimit = 3;

    public const int QuietPlyLimit = 80;

    private readonly Dictionary<string, int> _positionCounts = new();
    private int _quietPlies;

    public CheckersGame() => Reset();

    public static CheckersGame FromPosition(Board board, Side sideToMove)
    {
        var game = new CheckersGame();
        game.Initialize(board.Clone(), sideToMove);
        return game;
    }

    public Board Board { get; private set; } = Board.CreateInitial();

    public Side SideToMove { get; private set; } = Side.White;

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
