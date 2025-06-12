using System;
using System.Threading.Tasks;
using App.Grpc;
using App.Server;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace App.GrpcServer
{
    public class CheckersEvaluationServiceImpl : CheckersEvaluationService.CheckersEvaluationServiceBase
    {
        private readonly ILogger<CheckersEvaluationServiceImpl> _logger;
        private readonly IBoardEvaluator _evaluator;
        
        public CheckersEvaluationServiceImpl(ILogger<CheckersEvaluationServiceImpl> logger, IBoardEvaluator evaluator)
        {
            _logger = logger;
            _evaluator = evaluator;
        }

        public override async Task<MinimaxResponse> MinimaxSearch(MinimaxRequest request, ServerCallContext context)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation($"[{startTime:HH:mm:ss.fff}] Received Minimax request with depth {request.Depth}");
                
                // Create a board from the request data
                var board = new CheckersBoard();
                
                // Copy the board data from the request
                if (request.Board.Count >= 3)
                {
                    board.board[0] = request.Board[0];
                    board.board[1] = request.Board[1];
                    board.board[2] = request.Board[2];
                }
                else
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid board data received"));
                }
                
                // Create a Minimax instance to perform the calculation
                var minimax = new Minimax(request.Depth, 0, _evaluator);
                
                // Perform the Minimax search
                int score = minimax.MinimaxSearch(board, request.Depth, request.IsMaximizing);
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                
                _logger.LogInformation($"[{endTime:HH:mm:ss.fff}] Completed Minimax search with score {score}. Duration: {duration.TotalMilliseconds:F2}ms");
                
                // Return the result
                return new MinimaxResponse { Score = score };
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                _logger.LogError($"[{endTime:HH:mm:ss.fff}] Error in MinimaxSearch after {duration.TotalMilliseconds:F2}ms: {ex.Message}");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }
    }
}
