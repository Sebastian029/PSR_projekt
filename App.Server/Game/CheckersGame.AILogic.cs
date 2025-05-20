
namespace App.Server;

public partial class CheckersGame
{
    public (int fromField, int toField) GetAIMove()
    {
            {
                return checkersAi.CalculateOptimalMove(board, isWhiteTurn);
            }
      
    }
}