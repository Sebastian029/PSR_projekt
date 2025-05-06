using App.Server;
using Grpc.Core;
using GrpcServer;

namespace GrpcService
{
    public class CheckersServiceImpl : CheckersService.CheckersServiceBase
    {
        public override Task<MoveResponse> GetBestValue(BoardStateRequest request, ServerCallContext context)
        {
            try
            {
                var tmpBoard = new CheckersBoard();
                tmpBoard.board = request.BoardState.ToArray();

                // Use the requested depth if provided, otherwise default to 5
                int depth = request.Depth > 0 ? request.Depth : 5;
                int granulation = request.Granulation > 0 ? request.Granulation : 1;

                var ai = new CheckersAI(depth, granulation);

                var (fromField, toField) = ai.CalculateOptimalMove(tmpBoard, request.IsWhiteTurn);
                Console.WriteLine("Client - " + fromField + " to " + toField);
                
                return Task.FromResult(new MoveResponse
                {
                    FromField = fromField,
                    ToField = toField,
                    Success = true,
                    Message = "Move calculated"
                });
            }
            catch (Exception ex)
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
}