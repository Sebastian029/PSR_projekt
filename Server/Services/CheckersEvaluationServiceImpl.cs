using System.Diagnostics;
using App.Grpc;

using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using App.Server;
using Google.Protobuf.WellKnownTypes;

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
            var currentTime = DateTimeOffset.Now;
            var requestTime = request.RequestTime.ToDateTimeOffset();
            TimeSpan elapsed = currentTime - requestTime;
    
            Console.WriteLine($"Request time: {elapsed.TotalMilliseconds} ms");     
            
            var board = new CheckersBoard();

            if (request.Board.Count >= 3)
            {
                board.board[0] = request.Board[0];
                board.board[1] = request.Board[1];
                board.board[2] = request.Board[2];
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var minimax = new Minimax(request.Depth, _evaluator);
            int score = minimax.MinimaxSearch(board, request.Depth, request.IsMaximizing);
            stopwatch.Stop();
            Console.WriteLine($"Calculation time: {stopwatch.ElapsedMilliseconds} ms");     
        
            return Task.FromResult(new MinimaxResponse { Score = score, ResponseTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
            });
        }
    }
}