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
        private readonly MinimaxServer.IBoardEvaluator _evaluator; 

        public CheckersEvaluationServiceImpl(ILogger<CheckersEvaluationServiceImpl> logger, MinimaxServer.IBoardEvaluator evaluator)
        {
            _logger = logger;
            _evaluator = evaluator;
        }

        public override Task<MinimaxResponse> MinimaxSearch(MinimaxRequest request, ServerCallContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation($"Received Minimax request with depth {request.Depth}");

            try
            {
                var board = BoardConverter.ConvertFrom32Format(request.Board.ToArray());
                _logger.LogInformation($"Board conversion completed in {stopwatch.ElapsedMilliseconds}ms");

                var minimax = new Minimax(request.Depth, _evaluator);
                
                int score = minimax.MinimaxSearch(board, request.Depth, request.IsMaximizing);
                
                stopwatch.Stop();
                _logger.LogInformation($"Calculation completed in {stopwatch.ElapsedMilliseconds}ms with score {score}");

                return Task.FromResult(new MinimaxResponse 
                { 
                    Score = score,
                    ResponseTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now),
                    ServerComputationTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Error processing Minimax request: {ex.Message}");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }
    }
}
