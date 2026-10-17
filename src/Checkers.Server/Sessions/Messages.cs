using System.Text.Json;
using System.Text.Json.Serialization;
using Checkers.Core.Ai;
using Checkers.Core.Game;
using Checkers.Core.Model;
using ModelBoard = Checkers.Core.Model.Board;

namespace Checkers.Server.Sessions;

public sealed class ClientMessage
{
    public string Type { get; set; } = string.Empty;

    public int? Depth { get; set; }

    public int? Granulation { get; set; }

    public bool? IsPerformanceTest { get; set; }

    public bool? IsPlayerMode { get; set; }

    public int FromRow { get; set; }

    public int FromCol { get; set; }

    public int ToRow { get; set; }

    public int ToCol { get; set; }
}

public sealed record AiMoveStats(long ElapsedMs, long Nodes, int Jobs, int Score, bool Forced);

public sealed record GameStateMessage(
    bool Success,
    string[] Board,
    bool IsWhiteTurn,
    bool GameOver,
    string? Winner,
    string? DrawReason,
    string CurrentPlayer,
    bool MustContinueCapture,
    AiMoveStats? LastAiMove)
{
    public static GameStateMessage From(CheckersGame game, bool playerMode, bool success, AiMoveStats? lastAi) => new(
        success,
        SerializeBoard(game.Board),
        game.SideToMove == Side.White,
        game.IsOver,
        game.Status switch
        {
            GameStatus.WhiteWon => "white",
            GameStatus.BlackWon => "black",
            GameStatus.Draw => "draw",
            _ => null,
        },
        game.DrawReason,
        playerMode && game.SideToMove == Side.White ? "human" : "computer",
        game.MustContinueFrom is not null,
        lastAi);

    public static string[] SerializeBoard(ModelBoard board)
    {
        var cells = new string[ModelBoard.Size * ModelBoard.Size];
        for (var row = 0; row < ModelBoard.Size; row++)
        {
            for (var col = 0; col < ModelBoard.Size; col++)
            {
                var square = new Square(row, col);
                cells[row * ModelBoard.Size + col] = !square.IsDark
                    ? "invalid"
                    : board.Get(square) switch
                    {
                        Piece.WhiteMan => "white",
                        Piece.WhiteKing => "whiteKing",
                        Piece.BlackMan => "black",
                        Piece.BlackKing => "blackKing",
                        _ => "empty",
                    };
            }
        }

        return cells;
    }
}

public sealed record ErrorMessage([property: JsonPropertyName("error")] string Error);

public sealed record GameSettings(int Depth, int Granulation, bool IsPerformanceTest, bool IsPlayerMode)
{
    public SearchOptions ToSearchOptions() => new(Depth, Granulation);
}

public static class Json
{
    public static readonly JsonSerializerOptions Read = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static readonly JsonSerializerOptions Write = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
}
