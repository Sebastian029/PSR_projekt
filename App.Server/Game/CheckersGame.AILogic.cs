using GrpcServer;

namespace App.Server;

public partial class CheckersGame
{
    public (int fromField, int toField) GetAIMove()
    {
            {
                return checkersAi.GetBestMove(board, isWhiteTurn);
            }
      
    }
}