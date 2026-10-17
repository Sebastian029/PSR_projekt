using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Tests;

public class MoveGeneratorTests
{
    private const string Blank = "........";

    [Fact]
    public void WhiteHasSevenOpeningMoves()
    {
        var moves = MoveGenerator.Generate(Board.CreateInitial(), Side.White);

        Assert.Equal(7, moves.Count);
        Assert.All(moves, move => Assert.False(move.IsCapture));
    }

    [Fact]
    public void BlackMovesDownTheBoard()
    {
        var moves = MoveGenerator.Generate(Board.CreateInitial(), Side.Black);

        Assert.Equal(7, moves.Count);
        Assert.All(moves, move => Assert.Equal(move.From.Row + 1, move.To.Row));
    }

    [Fact]
    public void CaptureIsMandatory()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, "...b....", "..w.....", Blank, Blank);

        var moves = MoveGenerator.Generate(board, Side.White);

        var move = Assert.Single(moves);
        Assert.Equal(new Square(5, 2), move.From);
        Assert.Equal(new Square(3, 4), move.To);
        Assert.Equal([new Square(4, 3)], move.Captured);
    }

    [Fact]
    public void ManCannotCaptureBackwards()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, "...w....", "..b.....", Blank, Blank);

        var moves = MoveGenerator.Generate(board, Side.White);

        Assert.All(moves, move => Assert.False(move.IsCapture));
        Assert.Equal(2, moves.Count);
    }

    [Fact]
    public void ManKeepsCapturingAlongTheSequence()
    {
        var board = Board.Parse(Blank, Blank, Blank, "....b...", Blank, "..b.....", ".w......", Blank);

        var moves = MoveGenerator.Generate(board, Side.White);

        var move = Assert.Single(moves);
        Assert.Equal(new Square(6, 1), move.From);
        Assert.Equal([new Square(4, 3), new Square(2, 5)], move.Path);
        Assert.Equal([new Square(5, 2), new Square(3, 4)], move.Captured);
    }

    [Fact]
    public void BranchingCapturesProduceOneMovePerRoute()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, "...b.b..", "....w...", Blank, Blank);

        var moves = MoveGenerator.Generate(board, Side.White);

        Assert.Equal(2, moves.Count);
        Assert.Contains(moves, m => m.To == new Square(3, 2));
        Assert.Contains(moves, m => m.To == new Square(3, 6));
    }

    [Fact]
    public void PromotionEndsTheCaptureSequence()
    {
        var board = Board.Parse(Blank, "..b.b...", ".w......", Blank, Blank, Blank, Blank, Blank);

        var moves = MoveGenerator.Generate(board, Side.White);

        var move = Assert.Single(moves);
        Assert.Equal([new Square(0, 3)], move.Path);
        Assert.Equal(Piece.WhiteKing, MoveGenerator.Apply(board, move).Get(0, 3));
    }

    [Fact]
    public void KingMovesAnyDistanceAlongDiagonals()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, Blank, Blank, Blank, "W.......");

        var moves = MoveGenerator.Generate(board, Side.White);

        Assert.Equal(7, moves.Count);
        Assert.Contains(moves, m => m.To == new Square(0, 7));
    }

    [Fact]
    public void KingCapturesFromADistanceAndMayLandOnAnyFreeSquareBehind()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, "...b....", Blank, Blank, "W.......");

        var moves = MoveGenerator.Generate(board, Side.White);

        Assert.Equal(4, moves.Count);
        Assert.All(moves, move => Assert.Equal([new Square(4, 3)], move.Captured));
        Assert.Equal(
            [new Square(3, 4), new Square(2, 5), new Square(1, 6), new Square(0, 7)],
            moves.Select(m => m.To).OrderByDescending(s => s.Row));
    }

    [Fact]
    public void KingCannotJumpOverTwoPiecesInARow()
    {
        var board = Board.Parse(Blank, Blank, Blank, "....b...", "...b....", Blank, Blank, "W.......");

        var moves = MoveGenerator.Generate(board, Side.White);

        Assert.DoesNotContain(moves, move => move.IsCapture);
    }

    [Fact]
    public void ContinuationIsRestrictedToTheCapturingPiece()
    {
        var board = Board.Parse(Blank, Blank, Blank, Blank, "...b....", "..w...b.", ".......w", Blank);

        var moves = MoveGenerator.Generate(board, Side.White, mustContinueFrom: new Square(5, 2));

        Assert.NotEmpty(moves);
        Assert.All(moves, move => Assert.Equal(new Square(5, 2), move.From));
        Assert.All(moves, move => Assert.True(move.IsCapture));
    }

    [Fact]
    public void ApplyDoesNotMutateTheInputBoard()
    {
        var board = Board.CreateInitial();
        var snapshot = board.Clone();

        MoveGenerator.Apply(board, MoveGenerator.Generate(board, Side.White)[0]);

        Assert.Equal(snapshot, board);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void HasMovesAgreesWithGenerateOnRandomPositions(int seed)
    {
        for (var plies = 0; plies < 60; plies += 3)
        {
            var (board, side) = TestSupport.RandomPosition(seed, plies);

            Assert.Equal(MoveGenerator.Generate(board, side).Count > 0, MoveGenerator.HasMoves(board, side));
            Assert.Equal(
                MoveGenerator.Generate(board, side.Opposite()).Count > 0,
                MoveGenerator.HasMoves(board, side.Opposite()));
        }
    }
}
