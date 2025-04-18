using GrpcServer;

namespace App.Server;

public partial class CheckersGame
{
    public (int fromField, int toField) GetAIMove()
    {
        try
        {
            var request = new BoardStateRequest
            {
                BoardState = { board.board },
                IsWhiteTurn = isWhiteTurn,
            };

            var response = _client.GetBestMove(request);

            if (response.Success)
            {
                return (response.FromField, response.ToField);
            }
            else
            {
                return aIntelligence.GetBestMove(board, isWhiteTurn);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"gRPC call failed: {ex.Message}");
            return (-1, -1);
        }
    }
}