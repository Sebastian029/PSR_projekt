using App.Grpc;
using App.Server;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;

namespace App.Client
{
    public class MinimaxDistributor : IDisposable
    {
        private readonly List<string> _serverAddresses;
        private readonly Dictionary<string, GrpcChannel> _channels;
        private readonly object _lockObject = new object();
        private int _currentServerIndex = 0;

        public MinimaxDistributor(List<string> serverAddresses)
        {
            _serverAddresses = serverAddresses;
            _channels = new Dictionary<string, GrpcChannel>();
            
            // Sprawdź dostępność wysokiej rozdzielczości
            if (!Stopwatch.IsHighResolution)
            {
                Console.WriteLine("Warning: High-resolution timing not available");
            }
            
            var channelOptions = new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 4 * 1024 * 1024, // 4MB
                MaxSendMessageSize = 4 * 1024 * 1024,    // 4MB
                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true,
                    MaxConnectionsPerServer = 10
                }
            };
            
            foreach (var address in _serverAddresses)
            {
                _channels[address] = GrpcChannel.ForAddress(address, channelOptions);
            }
        }

        public async Task<List<int>> ProcessTasksWithRoundRobin(
            List<(CheckersBoard board, int depth, bool isMaximizing)> allTasks)
        {
            // Use array to maintain order instead of ConcurrentBag
            var results = new int[allTasks.Count];
            var activeTasks = new Dictionary<string, (Task<int> task, int index)>();
            var taskQueue = new Queue<(CheckersBoard, int, bool, int)>();
            
            // Add indices to maintain order
            for (int i = 0; i < allTasks.Count; i++)
            {
                var task = allTasks[i];
                taskQueue.Enqueue((task.board, task.depth, task.isMaximizing, i));
            }
            
            // Initially send one task to each server
            foreach (var serverAddress in _serverAddresses)
            {
                if (taskQueue.Count > 0)
                {
                    var (board, depth, isMaximizing, index) = taskQueue.Dequeue();
                    var task = SendTaskToSpecificServer(board, depth, isMaximizing, serverAddress);
                    activeTasks[serverAddress] = (task, index);
                }
            }
            
            // Process remaining tasks as servers become available
            while (activeTasks.Count > 0)
            {
                var completedTaskPair = await Task.WhenAny(activeTasks.Values.Select(x => x.task));
                var completedEntry = activeTasks.First(kvp => kvp.Value.task == completedTaskPair);
                var completedServer = completedEntry.Key;
                var resultIndex = completedEntry.Value.index;
                
                // Store result at correct index to maintain order
                results[resultIndex] = await completedTaskPair;
                activeTasks.Remove(completedServer);
                
                // Send next task to the now-free server
                if (taskQueue.Count > 0)
                {
                    var (board, depth, isMaximizing, index) = taskQueue.Dequeue();
                    var nextTask = SendTaskToSpecificServer(board, depth, isMaximizing, completedServer);
                    activeTasks[completedServer] = (nextTask, index);
                }
            }
            
            return results.ToList();
        }

        private string GetNextServerAddress()
        {
            lock (_lockObject)
            {
                var serverAddress = _serverAddresses[_currentServerIndex];
                _currentServerIndex = (_currentServerIndex + 1) % _serverAddresses.Count;
                return serverAddress;
            }
        }

        private async Task<int> SendTaskToSpecificServer(CheckersBoard board, int depth, bool isMaximizing, string serverAddress)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var channel = _channels[serverAddress];
            var client = new CheckersEvaluationService.CheckersEvaluationServiceClient(channel);
            
            var request = new MinimaxRequest
            {
                Depth = depth,
                IsMaximizing = isMaximizing,
                // Use deterministic timestamp instead of DateTimeOffset.Now
                RequestTime = Timestamp.FromDateTimeOffset(DateTimeOffset.UnixEpoch)
            };

            // Precyzyjny pomiar konwersji
            var conversionStopwatch = Stopwatch.StartNew();
            var compressedBoard = ConvertBoardTo32Format(board);
            conversionStopwatch.Stop();
            double conversionTimeMs = PreciseTimer.GetElapsedMilliseconds(conversionStopwatch);
            
            request.Board.Add(compressedBoard[0]);
            request.Board.Add(compressedBoard[1]);
            request.Board.Add(compressedBoard[2]);

            try
            {
                // Precyzyjny pomiar czasu komunikacji sieciowej
                var networkStopwatch = Stopwatch.StartNew();
                var response = await client.MinimaxSearchAsync(request);
                networkStopwatch.Stop();
                double networkTimeMs = PreciseTimer.GetElapsedMilliseconds(networkStopwatch);
                
                totalStopwatch.Stop();
                double totalTimeMs = PreciseTimer.GetElapsedMilliseconds(totalStopwatch);
                double computationTimeMs = totalTimeMs - conversionTimeMs - networkTimeMs;
                if (computationTimeMs < 0) computationTimeMs = 0;
                
                Console.WriteLine($"Server {serverAddress} - Total: {totalTimeMs:F2}ms, Network: {networkTimeMs:F2}ms, Computation: {computationTimeMs:F2}ms, Score: {response.Score}");
                
                GameLogger.LogMinimaxOperation(
                    "SendTaskToSpecificServer", 
                    serverAddress, 
                    depth, 
                    isMaximizing, 
                    (long)totalTimeMs, 
                    (long)conversionTimeMs, 
                    (long)networkTimeMs, 
                    (long)computationTimeMs, 
                    response.Score);
                
                return response.Score;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with server {serverAddress}: {ex.Message}");
                
                totalStopwatch.Stop();
                double totalTimeMs = PreciseTimer.GetElapsedMilliseconds(totalStopwatch);
                GameLogger.LogMinimaxOperation(
                    "ServerFailure", 
                    serverAddress, 
                    depth, 
                    isMaximizing, 
                    (long)totalTimeMs, 
                    (long)conversionTimeMs, 
                    0, 
                    0, 
                    0);
                
                throw; // Re-throw to handle at higher level
            }
        }

        private uint[] ConvertBoardTo32Format(CheckersBoard board)
        {
            uint[] result = new uint[3];
            int fieldIndex = 0;

            // Convert from 8x8 to 32-field format
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        if (fieldIndex >= 32) break;

                        PieceType piece = board.GetPiece(row, col);
                        byte pieceValue = ConvertPieceTypeToByte(piece);
                        
                        // Use 4 bits per field (compatible with server)
                        int boardIndex = fieldIndex / 8;
                        int bitPosition = (fieldIndex % 8) * 4;
                        
                        if (boardIndex < 3)
                        {
                            uint mask = 0xFu << bitPosition;
                            result[boardIndex] = (result[boardIndex] & ~mask) | ((uint)pieceValue << bitPosition);
                        }
                        
                        fieldIndex++;
                    }
                }
            }

            Console.WriteLine($"Converted {fieldIndex} fields to 32-format board");
            return result;
        }

        private bool IsDarkSquare(int row, int col)
        {
            return (row + col) % 2 == 1;
        }

        private byte ConvertPieceTypeToByte(PieceType piece)
        {
            return piece switch
            {
                PieceType.Empty => 0,
                PieceType.WhitePawn => 1,
                PieceType.WhiteKing => 2,
                PieceType.BlackPawn => 3,
                PieceType.BlackKing => 4,
                _ => 0
            };
        }

        // Validation method for determinism testing
        public async Task<bool> ValidateDeterminism(CheckersBoard board, int depth, bool isMaximizing, int iterations = 3)
        {
            var results = new List<List<int>>();
            
            for (int i = 0; i < iterations; i++)
            {
                var tasks = new List<(CheckersBoard, int, bool)> { (board, depth, isMaximizing) };
                var result = await ProcessTasksWithRoundRobin(tasks);
                results.Add(result);
                
                Console.WriteLine($"Iteration {i + 1}: Score = {result[0]}");
            }
            
            // Check if all results are identical
            var firstResult = results[0];
            bool allIdentical = results.All(r => r.SequenceEqual(firstResult));
            
            Console.WriteLine($"Determinism test: {(allIdentical ? "PASSED" : "FAILED")}");
            return allIdentical;
        }

        // Batch validation for multiple board states
        public async Task<bool> ValidateBatchDeterminism(
            List<(CheckersBoard board, int depth, bool isMaximizing)> tasks, 
            int iterations = 2)
        {
            var allResults = new List<List<int>>();
            
            for (int i = 0; i < iterations; i++)
            {
                var result = await ProcessTasksWithRoundRobin(tasks);
                allResults.Add(result);
                
                Console.WriteLine($"Batch iteration {i + 1}: Results = [{string.Join(", ", result)}]");
            }
            
            // Check if all batch results are identical
            var firstBatch = allResults[0];
            bool allBatchesIdentical = allResults.All(batch => batch.SequenceEqual(firstBatch));
            
            Console.WriteLine($"Batch determinism test: {(allBatchesIdentical ? "PASSED" : "FAILED")}");
            return allBatchesIdentical;
        }

        public Dictionary<string, bool> GetServerStatus()
        {
            var status = new Dictionary<string, bool>();
            foreach (var address in _serverAddresses)
            {
                status[address] = _channels.ContainsKey(address) && _channels[address] != null;
            }
            return status;
        }

        public void Dispose()
        {
            Console.WriteLine("Saving final summary...");
            GameLogger.WriteMinimaxSummary();
            
            foreach (var channel in _channels.Values)
            {
                channel?.Dispose();
            }
            _channels.Clear();
        }
    }

    // Klasa pomocnicza dla precyzyjnych pomiarów czasu
    public static class PreciseTimer
    {
        public static double GetElapsedMilliseconds(Stopwatch stopwatch)
        {
            return (double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
        }
        
        public static void LogTimingInfo()
        {
            Console.WriteLine($"Timer frequency: {Stopwatch.Frequency} Hz");
            Console.WriteLine($"High resolution: {Stopwatch.IsHighResolution}");
            Console.WriteLine($"Precision: {(double)1 / Stopwatch.Frequency * 1000000:F2} microseconds");
        }
    }
}
