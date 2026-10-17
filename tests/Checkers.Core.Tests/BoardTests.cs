using Checkers.Core.Model;

namespace Checkers.Core.Tests;

public class BoardTests
{
    [Fact]
    public void InitialBoardHasTwelvePiecesPerSideOnDarkSquares()
    {
        var board = Board.CreateInitial();

        Assert.Equal(12, board.Count(Side.White));
        Assert.Equal(12, board.Count(Side.Black));
        foreach (var square in board.PiecesOf(Side.White).Concat(board.PiecesOf(Side.Black)))
        {
            Assert.True(square.IsDark);
        }

        Assert.Equal(Piece.BlackMan, board.Get(0, 1));
        Assert.Equal(Piece.WhiteMan, board.Get(7, 0));
    }

    [Fact]
    public void WireFormatRoundTripsEveryDarkSquare()
    {
        // Regression: the legacy 3-word packing silently dropped the last 8 dark squares (rows 6-7).
        var board = Board.Empty();
        var piece = Piece.WhiteMan;
        for (var row = 0; row < Board.Size; row++)
        {
            for (var col = 0; col < Board.Size; col++)
            {
                var square = new Square(row, col);
                if (!square.IsDark)
                {
                    continue;
                }

                board.Set(square, piece);
                piece = piece == Piece.BlackKing ? Piece.WhiteMan : piece + 1;
            }
        }

        var wire = board.ToWire();

        Assert.Equal(Board.DarkSquareCount, wire.Length);
        Assert.Equal(board, Board.FromWire(wire));
    }

    [Fact]
    public void FromWireRejectsWrongLength()
    {
        Assert.Throws<ArgumentException>(() => Board.FromWire(new byte[24]));
    }

    [Fact]
    public void FromWireRejectsUnknownPieceCode()
    {
        var wire = new byte[Board.DarkSquareCount];
        wire[5] = 9;

        Assert.Throws<ArgumentException>(() => Board.FromWire(wire));
    }

    [Fact]
    public void ParseRejectsPieceOnLightSquare()
    {
        var rows = new[] { "w.......", "........", "........", "........", "........", "........", "........", "........" };

        Assert.Throws<ArgumentException>(() => Board.Parse(rows));
    }

    [Fact]
    public void GetOutsideTheBoardReturnsEmptyAndSetThrows()
    {
        var board = Board.Empty();

        Assert.Equal(Piece.Empty, board.Get(-1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.Set(new Square(8, 0), Piece.WhiteMan));
    }

    [Fact]
    public void CloneIsIndependent()
    {
        var original = Board.CreateInitial();
        var clone = original.Clone();

        clone.Set(new Square(0, 1), Piece.Empty);

        Assert.Equal(Piece.BlackMan, original.Get(0, 1));
        Assert.NotEqual(original, clone);
    }
}
