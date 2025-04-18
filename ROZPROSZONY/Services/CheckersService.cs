using App.Server;
using Grpc.Core;
using GrpcServer;
using GrpcService;


public class CheckersServiceImpl : CheckersService.CheckersServiceBase
{
    // private readonly CheckersGame _checkersGame;
    //
    // public CheckersServiceImpl(CheckersGame checkersGame)
    // {
    //     _checkersGame = checkersGame;
    // }

    public override Task<MoveResponse> GetBestMove(BoardStateRequest request, ServerCallContext context)
    {
        try
        {
            var tmpBoard = new CheckersBoard();

            tmpBoard.board = request.BoardState.ToArray();
            
            var ai = new CheckersAI();
            
            var (fromField, toField) = ai.GetBestMove(tmpBoard, request.IsWhiteTurn);

            Console.WriteLine("Obliczam ruch!");
            Console.WriteLine($"Z {fromField} na {toField}");
            return Task.FromResult(new MoveResponse
            {
                FromField = fromField,
                ToField = toField,
                Success = true,
                Message = "Move calculated"
            });
        }
        catch(Exception ex)
        {
            return Task.FromResult(new MoveResponse
            {
                FromField = -1,
                ToField = -1,
                Success = false,
                Message = $"Error while calculating move: {ex.Message}"
            });
        }
    }
    
}