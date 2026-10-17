using Checkers.Core.Model;

namespace Checkers.Core.Ai;

public static class Evaluator
{
    public const int ManValue = 100;
    public const int KingValue = 300;

    private const int AdvancementBonus = 5;
    private const int EdgeBonus = 3;
    private const int BackRankBonus = 8;

    public static int Evaluate(Board board)
    {
        var score = 0;
        for (var row = 0; row < Board.Size; row++)
        {
            for (var col = 0; col < Board.Size; col++)
            {
                var piece = board.Get(row, col);
                if (piece == Piece.Empty)
                {
                    continue;
                }

                var side = piece.SideOf();
                var value = piece.IsKing() ? KingValue : ManValue;
                if (piece.IsMan())
                {
                    var advancement = side == Side.White ? Board.Size - 1 - row : row;
                    value += advancement * AdvancementBonus;
                    if (row == (side == Side.White ? Board.Size - 1 : 0))
                    {
                        value += BackRankBonus;
                    }
                }

                if (col is 0 or Board.Size - 1)
                {
                    value += EdgeBonus;
                }

                score += side == Side.White ? value : -value;
            }
        }

        return score;
    }
}
