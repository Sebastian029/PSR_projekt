using App.Server;
using Grpc.Net.Client;
using GrpcServer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CheckersWorker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Allow HTTP/2 without TLS for development
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            
            Console.WriteLine("Starting Checkers AI Worker...");
            
            // Create a unique worker ID
            string workerId = $"Worker-{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Connect to the gRPC server
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = new CheckersService.CheckersServiceClient(channel);
            
            // Register the worker with the server
            try
            {
                var registrationResponse = await client.RegisterWorkerAsync(new WorkerRegistration
                {
                    WorkerId = workerId,
                    MaxDepth = 8  // Configure as needed
                });
                
                if (registrationResponse.Success)
                {
                    Console.WriteLine($"Worker {workerId} registered successfully");
                }
                else
                {
                    Console.WriteLine("Worker registration failed");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering worker: {ex.Message}");
                return;
            }
            
            // Main worker loop
            while (true)
            {
                try
                {
                    // Request a task from the server
                    var taskRequest = await client.GetTaskAsync(new TaskRequest { WorkerId = workerId });
                    
                    // Check if we received a valid task
                    if (taskRequest == null || taskRequest.Request == null || string.IsNullOrEmpty(taskRequest.TaskId))
                    {
                        // No task available, wait before trying again
                        await Task.Delay(100);
                        continue;
                    }
                    
                    Console.WriteLine($"Received task {taskRequest.TaskId}");
                    
                    // Process the task
                    var result = CalculateBestMove(taskRequest.Request);
                    
                    // Submit the result back to the server
                    await client.SubmitResultAsync(new CalculationResult
                    {
                        TaskId = taskRequest.TaskId,
                        Result = result
                    });
                    
                    Console.WriteLine($"Completed task {taskRequest.TaskId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing task: {ex.Message}");
                    await Task.Delay(1000); // Wait before retrying
                }
            }
        }
        
        static BestValueResponse CalculateBestMove(BoardStateRequest request)
        {
            var workerStartTicks = DateTime.UtcNow.Ticks;
            
            try
            {
                // Create a board from the request
                var board = new CheckersBoard();
                board.board = request.BoardState.ToArray();
                
                // Create evaluator and move generator
                var evaluator = new EvaluatorClient();
                var moveGenerator = new MoveGeneratorClient();
                
                // Get valid moves
                var captures = moveGenerator.GetMandatoryCaptures(board, request.IsWhiteTurn);
                var moves = captures.Count > 0
                    ? moveGenerator.GetCaptureMoves(captures)
                    : moveGenerator.GetAllValidMoves(board, request.IsWhiteTurn);
                
                if (moves.Count == 0)
                {
                    return new BestValueResponse
                    {
                        Value = request.IsWhiteTurn ? int.MinValue : int.MaxValue,
                        FromField = -1,
                        ToField = -1,
                        Success = true,
                        WorkerStartTicks = workerStartTicks,
                        WorkerEndTicks = DateTime.UtcNow.Ticks
                    };
                }
                
                // Create minimax instance for local calculation
                var minimax = new MinimaxClient(request.Depth, request.Granulation, evaluator);
                
                // Find best move
                int bestValue = request.IsWhiteTurn ? int.MinValue : int.MaxValue;
                (int fromField, int toField) bestMove = (-1, -1);
                
                foreach (var (from, to) in moves)
                {
                    var simulated = board.Clone();
                    if (captures.Count > 0)
                        new CaptureSimulatorClient().SimulateCapture(simulated, from, to);
                    else
                        simulated.MovePiece(from, to);
                    
                    int currentValue = minimax.MinimaxSearch(simulated, request.Depth - 1, !request.IsWhiteTurn);
                    
                    if ((request.IsWhiteTurn && currentValue > bestValue) ||
                        (!request.IsWhiteTurn && currentValue < bestValue))
                    {
                        bestValue = currentValue;
                        bestMove = (from, to);
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
                Console.WriteLine($"Error calculating best move: {ex.Message}");
                return new BestValueResponse
                {
                    Value = 0,
                    FromField = -1,
                    ToField = -1,
                    Success = false,
                    WorkerStartTicks = workerStartTicks,
                    WorkerEndTicks = DateTime.UtcNow.Ticks
                };
            }
        }
    }
}
