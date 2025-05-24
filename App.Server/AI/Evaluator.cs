// Evaluator.cs
using App.Server;

public class Evaluator : IBoardEvaluator
{
    public int EvaluateBoard(CheckersBoard board, bool forWhite)
    {
        int score = 0;
        
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (!IsDarkSquare(row, col)) continue;
                
                PieceType piece = board.GetPiece(row, col);
                if (piece == PieceType.Empty) continue;

                bool isWhite = PieceUtils.IsWhite(piece);
                bool isKing = PieceUtils.IsKing(piece);
                
                int value = isKing ? 5 : 2;
                int positionalBonus = 0;

                if (!isKing)
                {
                    // Advancement: bardziej zaawansowane figury są lepsze
                    int advancement = isWhite ? (7 - row) : row;
                    positionalBonus += advancement;

                    // Obrona tylnego rzędu
                    if ((isWhite && row == 7) || (!isWhite && row == 0))
                        positionalBonus += 2;
                }

                // Bonus za bezpieczeństwo na krawędzi
                if (col == 0 || col == 7)
                    positionalBonus += 1;

                int totalPieceValue = value + positionalBonus;
                score += isWhite ? totalPieceValue : -totalPieceValue;
            }
        }

        return forWhite ? score : -score;
    }

    private bool IsDarkSquare(int row, int col)
    {
        return (row + col) % 2 == 1;
    }
}

public interface IBoardEvaluator
{
    int EvaluateBoard(CheckersBoard board, bool forWhite);
}