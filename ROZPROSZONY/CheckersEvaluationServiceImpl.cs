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

        public override Task<MinimaxResponse> MinimaxSearch(MinimaxRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Received Minimax request with depth {request.Depth}");
            
            // Create a board from the request data
            var board = new CheckersBoard();
            
            // Copy the board data from the request
            if (request.Board.Count >= 3)
            {
                board.board[0] = request.Board[0];
                board.board[1] = request.Board[1];
                board.board[2] = request.Board[2];
            }
            
            // Create a Minimax instance to perform the calculation
            var minimax = new Minimax(request.Depth,0, _evaluator);
            
            // Perform the Minimax search
            int score = minimax.MinimaxSearch(board, request.Depth, request.IsMaximizing);
            
            _logger.LogInformation($"Completed Minimax search with score {score}");
            
            // Return the result
            return Task.FromResult(new MinimaxResponse { Score = score });
        }
    }
}
