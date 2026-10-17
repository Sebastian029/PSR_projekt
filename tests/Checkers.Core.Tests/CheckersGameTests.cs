using Checkers.Core.Game;
using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Tests;

public class CheckersGameTests
{
    private const string Blank = "........";

    private static Square S(int row, int col) => new(row, col);

    [Fact]
    public void NewGameStartsWithWhiteToMove()
    {
        var game = new CheckersGame();

        Assert.Equal(Side.White, game.SideToMove);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(7, game.GetLegalMoves().Count);
    }

    [Fact]
    public void LegalQuietMoveSwitchesTurn()
    {
        var game = new CheckersGame();

        var result = game.TryPlayStep(S(5, 2), S(4, 3));

        Assert.True(result.Success);
        Assert.True(result.TurnEnded);
        Assert.Equal(Side.Black, game.SideToMove);
        Assert.Equal(Piece.WhiteMan, game.Board.Get(4, 3));
        Assert.Equal(Piece.Empty, game.Board.Get(5, 2));
    }

    [Theory]
    [InlineData(2, 1, 3, 2)] // black piece, white to move
    [InlineData(5, 2, 3, 4)] // two squares without a capture
    [InlineData(5, 2, 6, 3)] // backwards
    [InlineData(4, 4, 3, 3)] // empty source
    [InlineData(5, 2, 4, 2)] // light square
    [InlineData(5, 2, 8, 5)] // off the board
    public void IllegalStepsAreRejectedAndLeaveTheGameUntouched(int fromRow, int fromCol, int toRow, int toCol)
    {
        var game = new CheckersGame();
        var before = game.Board.Clone();

        var result = game.TryPlayStep(S(fromRow, fromCol), S(toRow, toCol));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Equal(before, game.Board);
        Assert.Equal(Side.White, game.SideToMove);
    }

    [Fact]
    public void CaptureMustBeTakenWhenAvailable()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, "...b....", "..w.w...", Blank, "w.......");
        var game = CheckersGame.FromPosition(board, Side.White);

        var quiet = game.TryPlayStep(S(7, 0), S(6, 1));
        var capture = game.TryPlayStep(S(5, 2), S(3, 4));

        Assert.False(quiet.Success);
        Assert.Contains("mandatory", quiet.Error);
        Assert.True(capture.Success);
        Assert.Equal(Piece.Empty, game.Board.Get(4, 3));
    }

    [Fact]
    public void MultiCaptureIsPlayedStepByStepWithTheSamePiece()
    {
        var board = Board.Parse(
            Blank, "b.......", Blank, "....b...", Blank, "..b.....", ".w...w..", Blank);
        var game = CheckersGame.FromPosition(board, Side.White);

        var first = game.TryPlayStep(S(6, 1), S(4, 3));

        Assert.True(first.Success);
        Assert.False(first.TurnEnded);
        Assert.Equal(S(4, 3), game.MustContinueFrom);
        Assert.Equal(Side.White, game.SideToMove);

        var otherPiece = game.TryPlayStep(S(6, 5), S(5, 4));
        Assert.False(otherPiece.Success);

        var second = game.TryPlayStep(S(4, 3), S(2, 5));
        Assert.True(second.Success);
        Assert.True(second.TurnEnded);
        Assert.Null(game.MustContinueFrom);
        Assert.Equal(Side.Black, game.SideToMove);
        Assert.Equal(1, game.Board.Count(Side.Black));
    }

    [Fact]
    public void PlayMoveAppliesAWholeSequence()
    {
        var board = Board.Parse(
            Blank, Blank, Blank, "....b...", Blank, "..b.....", ".w......", Blank);
        var game = CheckersGame.FromPosition(board, Side.White);

        game.PlayMove(game.GetLegalMoves().Single());

        Assert.Equal(Piece.WhiteMan, game.Board.Get(2, 5));
        Assert.Equal(0, game.Board.Count(Side.Black));
        Assert.Equal(GameStatus.WhiteWon, game.Status);
    }

    [Fact]
    public void PromotionEndsTheTurnEvenIfTheKingCouldCaptureAgain()
    {
        var board = Board.Parse(Blank, "..b.b...", ".w......", Blank, Blank, Blank, Blank, Blank);
        var game = CheckersGame.FromPosition(board, Side.White);

        var result = game.TryPlayStep(S(2, 1), S(0, 3));

        Assert.True(result.Success);
        Assert.True(result.TurnEnded);
        Assert.Equal(Piece.WhiteKing, game.Board.Get(0, 3));
        Assert.Equal(Side.Black, game.SideToMove);
    }

    [Fact]
    public void PlayerWithoutMovesLoses()
    {
        var board = Board.Parse(
            Blank, Blank, Blank, Blank, Blank, "w.w.....", ".b......", "w.w.....");
        var game = CheckersGame.FromPosition(board, Side.White);
        Assert.Equal(GameStatus.InProgress, game.Status);

        // White plays a quiet move; black is completely blocked and has nothing to move.
        Assert.True(game.TryPlayStep(S(5, 2), S(4, 3)).Success);

        Assert.True(game.IsOver);
        Assert.Equal(GameStatus.WhiteWon, game.Status);
        Assert.False(game.TryPlayStep(S(4, 3), S(3, 4)).Success);
    }

    [Fact]
    public void ThreefoldRepetitionIsADraw()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, Blank, Blank, Blank, "W.......");
        board.Set(S(0, 1), Piece.BlackKing);
        var game = CheckersGame.FromPosition(board, Side.White);

        for (var cycle = 0; cycle < 2; cycle++)
        {
            Assert.True(game.TryPlayStep(S(7, 0), S(6, 1)).Success);
            Assert.True(game.TryPlayStep(S(0, 1), S(1, 0)).Success);
            Assert.True(game.TryPlayStep(S(6, 1), S(7, 0)).Success);
            Assert.True(game.TryPlayStep(S(1, 0), S(0, 1)).Success);
        }

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Contains("repetition", game.DrawReason);
    }

    [Fact]
    public void ResetRestoresTheInitialState()
    {
        var game = new CheckersGame();
        game.TryPlayStep(S(5, 2), S(4, 3));

        game.Reset();

        Assert.Equal(Board.CreateInitial(), game.Board);
        Assert.Equal(Side.White, game.SideToMove);
        Assert.Equal(0, game.PlyCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RandomPlayoutsStayConsistent(int seed)
    {
        var random = new Random(seed);
        var game = new CheckersGame();

        for (var ply = 0; ply < 600 && !game.IsOver; ply++)
        {
            var moves = game.GetLegalMoves();
            Assert.NotEmpty(moves);
            var before = (game.Board.Count(Side.White), game.Board.Count(Side.Black));

            game.PlayMove(moves[random.Next(moves.Count)]);

            Assert.True(game.Board.Count(Side.White) <= before.Item1);
            Assert.True(game.Board.Count(Side.Black) <= before.Item2);
            Assert.Null(game.MustContinueFrom);
        }

        if (game.IsOver && game.Status != GameStatus.Draw)
        {
            Assert.False(MoveGenerator.HasMoves(game.Board, game.SideToMove));
        }
    }
}
