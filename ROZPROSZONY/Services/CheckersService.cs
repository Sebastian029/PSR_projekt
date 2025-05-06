using App.Server;
using Grpc.Core;
using GrpcServer;

namespace GrpcService
{
    public class CheckersServiceImpl : CheckersService.CheckersServiceBase
    {
        public override Task<BestValueResponse> GetBestValue(BoardStateRequest request, ServerCallContext context)
        {
            try
            {
                var tmpBoard = new CheckersBoard();
                tmpBoard.board = request.BoardState.ToArray();

                // Use the requested depth if provided, otherwise default to 5
                int depth = request.Depth > 0 ? request.Depth : 5;
                int granulation = request.Granulation > 0 ? request.Granulation : 1;

                // Utwórz ewaluator i AI
                var evaluator = new EvaluatorClient();
                var ai = new MinimaxClient (depth, granulation, evaluator);

                // Pobierz wszystkie możliwe ruchy
                var moveGenerator = new MoveGeneratorClient();
                var captures = moveGenerator.GetMandatoryCaptures(tmpBoard, request.IsWhiteTurn);
                var moves = captures.Count > 0
                    ? moveGenerator.GetCaptureMoves(captures).Select(m => (fromField: m.Item1, toField: m.Item2))
                    : moveGenerator.GetAllValidMoves(tmpBoard, request.IsWhiteTurn).Select(m => (fromField: m.Item1, toField: m.Item2));

                // Znajdź najlepszy ruch i jego wartość
                int bestValue = request.IsWhiteTurn ? int.MinValue : int.MaxValue;
                (int fromField, int toField) bestMove = (-1, -1);

                foreach (var move in moves)
                {
                    var simulatedBoard = tmpBoard.Clone();

                    if (captures.Count > 0)
                        new CaptureSimulatorClient().SimulateCapture(simulatedBoard, move.fromField, move.toField);
                    else
                        simulatedBoard.MovePiece(move.fromField, move.toField);

                    // Oceń pozycję
                    int currentValue = ai.MinimaxSearch(simulatedBoard, depth - 1, !request.IsWhiteTurn);

                    // Aktualizuj najlepszą wartość i ruch
                    if ((request.IsWhiteTurn && currentValue > bestValue) ||
                        (!request.IsWhiteTurn && currentValue < bestValue))
                    {
                        bestValue = currentValue;
                        bestMove = move;
                    }
                }

                Console.WriteLine($"Best move: {bestMove.fromField} to {bestMove.toField} with value {bestValue}");

                return Task.FromResult(new BestValueResponse
                {
                    Value = bestValue,
                    FromField = bestMove.fromField,
                    ToField = bestMove.toField,
                    Success = true,
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new BestValueResponse
                {
                    Value = request.IsWhiteTurn ? int.MinValue : int.MaxValue,
                    FromField = -1,
                    ToField = -1,
                    Success = false,
                });
            }
        }
    }
}