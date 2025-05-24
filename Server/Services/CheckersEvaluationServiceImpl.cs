using System.Diagnostics;
using App.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using MinimaxServer; // Dodaj using dla namespace

namespace MinimaxServer.Services
{
    public class CheckersEvaluationServiceImpl : CheckersEvaluationService.CheckersEvaluationServiceBase
    {
        private readonly ILogger<CheckersEvaluationServiceImpl> _logger;
        private readonly MinimaxServer.IBoardEvaluator _evaluator; // Pełna nazwa namespace

        public CheckersEvaluationServiceImpl(ILogger<CheckersEvaluationServiceImpl> logger, MinimaxServer.IBoardEvaluator evaluator)
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

            Console.WriteLine($"Received depth: {request.Depth}, IsMaximizing: {request.IsMaximizing}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Konwertuj z formatu 32-polowego na szachownicę 8x8
            var board = BoardConverter.ConvertFrom32Format(request.Board.ToArray());
            
            stopwatch.Stop();
            Console.WriteLine($"Board conversion time: {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();
            var minimax = new Minimax(request.Depth, _evaluator);
            int score = minimax.MinimaxSearch(board, request.Depth, request.IsMaximizing);
            stopwatch.Stop();
            
            Console.WriteLine($"Calculation time: {stopwatch.ElapsedMilliseconds} ms");
            //Console.WriteLine($"Final score: {score}");

            return Task.FromResult(new MinimaxResponse 
            { 
                Score = score, 
                ResponseTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
            });
        }
    }
}
