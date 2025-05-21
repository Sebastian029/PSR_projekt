using App.Grpc;

using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using App.Server;

namespace MinimaxServer.Services
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
            // Log connection information
            var clientIp = context.GetHttpContext().Connection.RemoteIpAddress;
            var userAgent = context.RequestHeaders.GetValue("user-agent");

            // Console.WriteLine($"{DateTime.Now} - Connection from {clientIp} - {userAgent}");
            // _logger.LogInformation($"Connection from {clientIp} - {userAgent}");
            // _logger.LogInformation($"Received Minimax request with depth {request.Depth}");

            // Create a board from the request data
            var board = new CheckersBoard();

            // Copy the board data from the request
            if (request.Board.Count >= 3)
            {
                board.board[0] = request.Board[0];
                board.board[1] = request.Board[1];
                board.board[2] = request.Board[2];
            }
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Create a Minimax instance to perform the calculation
            var minimax = new Minimax(request.Depth, _evaluator);
        
            // Perform the Minimax search
            int score = minimax.MinimaxSearch(board, request.Depth, request.IsMaximizing);

            _logger.LogInformation($"Score {score} - Execution time: {stopwatch.ElapsedMilliseconds} ms");

            // Return the result
            return Task.FromResult(new MinimaxResponse { Score = score });
        }
    }
}