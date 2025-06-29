using App.Server;


public class EvaluatorClient : IBoardEvaluatorClient
{
    public int EvaluateBoard(CheckersBoard board, bool forWhite)
    {
        int score = 0;

        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            if (piece == (byte)PieceType.Empty) continue;

            bool isWhite = PieceUtils.IsWhite(piece);
            bool isKing = PieceUtils.IsKing(piece);
            int value = isKing ? 5 : 2;

            int row = i / 4; 
            int col = (i % 4) * 2 + ((row % 2 == 0) ? 1 : 0); 

            int positionalBonus = 0;

            if (!isKing)
            {
                int advancement = isWhite ? (7 - row) : row;
                positionalBonus += advancement;

                if ((isWhite && row == 7) || (!isWhite && row == 0))
                    positionalBonus += 2;
            }

            if (col == 0 || col == 7)
                positionalBonus += 1;

            int totalPieceValue = value + positionalBonus;

            score += isWhite ? totalPieceValue : -totalPieceValue;
        }

        return forWhite ? score : -score;
    }


}

public interface IBoardEvaluatorClient
{
    int EvaluateBoard(CheckersBoard board, bool forWhite);
}
