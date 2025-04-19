using App.Server;


public class Evaluator : IBoardEvaluator
{
    public readonly int _granulation;

    public Evaluator(int granulation)
    {
        _granulation = granulation;
    }

    public int EvaluateBoard(CheckersBoard board, bool forWhite)
    {
        int score = 0, white = 0, black = 0;

        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            if (piece == (byte)PieceType.Empty) continue;

            bool isWhite = PieceUtils.IsWhite(piece);
            bool isKing = PieceUtils.IsKing(piece);
            int val = isKing ? 3 : 1;
            var (row, col) = PieceUtils.GetBoardPosition(i);
            int centerDist = Math.Abs(col - 3) + Math.Abs(row - 3);
            int advancement = isWhite ? (7 - row) : row;

            if (isWhite)
            {
                white += val;
                score += val * _granulation + advancement * _granulation / 4 - centerDist * _granulation / 6;
            }
            else
            {
                black += val;
                score -= val * _granulation + advancement * _granulation / 4 - centerDist * _granulation / 6;
            }
        }

        if (white + black < 10) score = score * 3 / 2;
        return forWhite ? score : -score;
    }
}

public interface IBoardEvaluator
{
    int EvaluateBoard(CheckersBoard board, bool forWhite);
}
