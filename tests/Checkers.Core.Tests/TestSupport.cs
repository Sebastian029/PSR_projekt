using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Tests;

internal static class TestSupport
{
    public static (Board Board, Side SideToMove) RandomPosition(int seed, int plies)
    {
        var random = new Random(seed);
        var board = Board.CreateInitial();
        var side = Side.White;
        for (var i = 0; i < plies; i++)
        {
            var moves = MoveGenerator.Generate(board, side);
            if (moves.Count == 0)
            {
                break;
            }

            board = MoveGenerator.Apply(board, moves[random.Next(moves.Count)]);
            side = side.Opposite();
        }

        return (board, side);
    }

    public static Board Mirror(Board board)
    {
        var mirrored = Board.Empty();
        for (var row = 0; row < Board.Size; row++)
        {
            for (var col = 0; col < Board.Size; col++)
            {
                var piece = board.Get(row, col);
                if (piece == Piece.Empty)
                {
                    continue;
                }

                var swapped = piece switch
                {
                    Piece.WhiteMan => Piece.BlackMan,
                    Piece.WhiteKing => Piece.BlackKing,
                    Piece.BlackMan => Piece.WhiteMan,
                    _ => Piece.WhiteKing,
                };
                mirrored.Set(new Square(Board.Size - 1 - row, Board.Size - 1 - col), swapped);
            }
        }

        return mirrored;
    }
}
