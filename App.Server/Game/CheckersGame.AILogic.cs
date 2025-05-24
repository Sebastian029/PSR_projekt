// CheckersGame.AILogic.cs
namespace App.Server;

public partial class CheckersGame
{
    public (int fromRow, int fromCol, int toRow, int toCol) GetAIMove()
    {
        return checkersAi.CalculateOptimalMove(board, isWhiteTurn);
    }
}