using App.Server;
using Grpc.Net.Client;
using GrpcServer;
using System;
using System.Threading.Tasks;

namespace GrpcService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Checkers AI Worker...");

            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = new CheckersService.CheckersServiceClient(channel);

            var workerId = Guid.NewGuid().ToString();

            await client.RegisterWorkerAsync(new WorkerRegistration
            {
                WorkerId = workerId,
                //MaxDepth = 5
            });

            Console.WriteLine($"Worker {workerId} ready for tasks");

            while (true)
            {
                try
                {

                    var task = await client.GetTaskAsync(new TaskRequest { WorkerId = workerId });
                    //Console.WriteLine($"Received task {task.TaskId}");
                    if (task.Request == null)
                    {
                        //Console.WriteLine("Received null request.");
                        continue;
                    }
                    var result = CalculateBestMove(task.Request);

                    await client.SubmitResultAsync(new CalculationResult
                    {
                        TaskId = task.TaskId,
                        Result = result
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    await Task.Delay(5000);
                }
            }
        }

        static BestValueResponse CalculateBestMove(BoardStateRequest request)
        {
            var workerStartTicks = DateTime.UtcNow.Ticks;
            try
            {
                var tmpBoard = new CheckersBoard();
                tmpBoard.board = request.BoardState.ToArray();
        
                // Use the depth from the request
                int depth = request.Depth;
                int granulation = request.Granulation > 0 ? request.Granulation : 1;
        
                var evaluator = new EvaluatorClient();
                // Create MinimaxClient with the dynamic depth
                var ai = new MinimaxClient(depth, granulation, evaluator);


                var moveGenerator = new MoveGeneratorClient();
                var captures = moveGenerator.GetMandatoryCaptures(tmpBoard, request.IsWhiteTurn);
                var moves = captures.Count > 0
                    ? moveGenerator.GetCaptureMoves(captures).Select(m => (fromField: m.Item1, toField: m.Item2))
                    : moveGenerator.GetAllValidMoves(tmpBoard, request.IsWhiteTurn).Select(m => (fromField: m.Item1, toField: m.Item2));

                int bestValue = request.IsWhiteTurn ? int.MinValue : int.MaxValue;
                (int fromField, int toField) bestMove = (-1, -1);

                foreach (var move in moves)
                {
                    var simulatedBoard = tmpBoard.Clone();

                    if (captures.Count > 0)
                        new CaptureSimulatorClient().SimulateCapture(simulatedBoard, move.fromField, move.toField);
                    else
                        simulatedBoard.MovePiece(move.fromField, move.toField);

                    int currentValue = ai.MinimaxSearch(simulatedBoard, depth - 1, !request.IsWhiteTurn);

                    if ((request.IsWhiteTurn && currentValue > bestValue) ||
                        (!request.IsWhiteTurn && currentValue < bestValue))
                    {
                        bestValue = currentValue;
                        bestMove = move;
                    }
                }

                var workerEndTicks = DateTime.UtcNow.Ticks;
                var computationTime = TimeSpan.FromTicks(workerEndTicks - workerStartTicks);
                Console.WriteLine($"Computation time: {computationTime.TotalMilliseconds}ms");

                return new BestValueResponse
                {
                    Value = bestValue,
                    FromField = bestMove.fromField,
                    ToField = bestMove.toField,
                    Success = true,
                    WorkerStartTicks = workerStartTicks,
                    WorkerEndTicks = workerEndTicks
                };
            }
            catch (Exception ex)
            {
                var workerEndTicks = DateTime.UtcNow.Ticks;
                Console.WriteLine($"Error during calculation: {ex.Message}");
                return new BestValueResponse
                {
                    Value = request.IsWhiteTurn ? int.MinValue : int.MaxValue,
                    FromField = -1,
                    ToField = -1,
                    Success = false,
                    WorkerStartTicks = workerStartTicks,
                    WorkerEndTicks = workerEndTicks
                };
            }
        }
    }


    }