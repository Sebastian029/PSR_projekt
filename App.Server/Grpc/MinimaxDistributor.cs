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
        private readonly ServerPerformanceTracker _performanceTracker;
        private readonly object _lockObject = new object();
        private int _currentServerIndex = 0;

        public MinimaxDistributor(List<string> serverAddresses)
        {
            _serverAddresses = serverAddresses;
            _channels = new Dictionary<string, GrpcChannel>();
            _performanceTracker = new ServerPerformanceTracker();
            
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
                _performanceTracker.RegisterServer(address);
            }
            
            // Sprawdź dostępność wysokiej rozdzielczości
            if (!Stopwatch.IsHighResolution)
            {
                Console.WriteLine("Warning: High-resolution timing not available");
            }
        }

        // Original single task method with precise timing
        public int DistributeMinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
        {
            var totalStopwatch = Stopwatch.StartNew();
            
            int result = SendBoardForEvaluation(board, depth, isMaximizing);
            
            totalStopwatch.Stop();
            double totalTimeMs = PreciseTimer.GetElapsedMilliseconds(totalStopwatch);
            
            GameLogger.LogMinimaxOperation(
                "DistributeMinimaxSearch", 
                "ALL", 
                depth, 
                isMaximizing, 
                (long)totalTimeMs, 
                0, 
                0, 
                0, 
                result);
            
            return result;
        }

        // Parallel task distribution method with precise timing
        public async Task<List<int>> DistributeMultipleMinimaxSearches(
            List<(CheckersBoard board, int depth, bool isMaximizing)> tasks)
        {
            var taskList = new List<Task<int>>();
            
            // Send tasks to servers in round-robin fashion
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var serverAddress = GetNextServerAddress();
                
                taskList.Add(SendTaskToSpecificServer(task.board, task.depth, task.isMaximizing, serverAddress));
            }
            
            // Wait for all results
            var results = await Task.WhenAll(taskList);
            return results.ToList();
        }

        // Round-robin with continuous task processing
        public async Task<List<int>> ProcessTasksWithRoundRobin(
            List<(CheckersBoard board, int depth, bool isMaximizing)> allTasks)
        {
            var results = new ConcurrentBag<int>();
            var activeTasks = new Dictionary<string, Task<int>>();
            var taskQueue = new Queue<(CheckersBoard, int, bool)>(allTasks);
            
            // Initially send one task to each server
            foreach (var serverAddress in _serverAddresses)
            {
                if (taskQueue.Count > 0)
                {
                    var task = taskQueue.Dequeue();
                    activeTasks[serverAddress] = SendTaskToSpecificServer(task.Item1, task.Item2, task.Item3, serverAddress);
                }
            }
            
            // Process remaining tasks as servers become available
            while (activeTasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(activeTasks.Values);
                var completedServer = activeTasks.First(kvp => kvp.Value == completedTask).Key;
                
                // Collect result
                results.Add(await completedTask);
                activeTasks.Remove(completedServer);
                
                // Send next task to the now-free server
                if (taskQueue.Count > 0)
                {
                    var nextTask = taskQueue.Dequeue();
                    activeTasks[completedServer] = SendTaskToSpecificServer(
                        nextTask.Item1, nextTask.Item2, nextTask.Item3, completedServer);
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
                RequestTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
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
                _performanceTracker.StartRequest(serverAddress);
                
                // Precyzyjny pomiar czasu komunikacji sieciowej
                var networkStopwatch = Stopwatch.StartNew();
                var response = await client.MinimaxSearchAsync(request);
                networkStopwatch.Stop();
                double networkTimeMs = PreciseTimer.GetElapsedMilliseconds(networkStopwatch);
                
                _performanceTracker.UpdateMetrics(serverAddress, networkTimeMs);
                
                totalStopwatch.Stop();
                double totalTimeMs = PreciseTimer.GetElapsedMilliseconds(totalStopwatch);
                
                long serverComputationTimeMs = response.ServerComputationTimeMs;
                double clientOverheadMs = totalTimeMs - conversionTimeMs - networkTimeMs;
                if (clientOverheadMs < 0) clientOverheadMs = 0;
                
                Console.WriteLine($"Server {serverAddress} - Total: {totalTimeMs:F2}ms, Network: {networkTimeMs:F2}ms, Client overhead: {clientOverheadMs:F2}ms, Score: {response.Score}");
                
                GameLogger.LogMinimaxOperation(
                    "SendTaskToSpecificServer", 
                    serverAddress, 
                    depth, 
                    isMaximizing, 
                    (long)totalTimeMs, 
                    (long)conversionTimeMs, 
                    (long)networkTimeMs, 
                    serverComputationTimeMs, 
                    response.Score);
                
                return response.Score;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with server {serverAddress}: {ex.Message}");
                _performanceTracker.MarkServerUnavailable(serverAddress);
                
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

        // Original method with precise timing for backward compatibility
        private int SendBoardForEvaluation(CheckersBoard board, int depth, bool isMaximizing)
        {
            var totalStopwatch = Stopwatch.StartNew();
            string serverAddress = _performanceTracker.GetBestServer();
            var channel = _channels[serverAddress];
            var client = new CheckersEvaluationService.CheckersEvaluationServiceClient(channel);
            
            var request = new MinimaxRequest
            {
                Depth = depth,
                IsMaximizing = isMaximizing,
                RequestTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
            };

            var conversionStopwatch = Stopwatch.StartNew();
            var compressedBoard = ConvertBoardTo32Format(board);
            conversionStopwatch.Stop();
            double conversionTimeMs = PreciseTimer.GetElapsedMilliseconds(conversionStopwatch);
            
            request.Board.Add(compressedBoard[0]);
            request.Board.Add(compressedBoard[1]);
            request.Board.Add(compressedBoard[2]);

            try
            {
                _performanceTracker.StartRequest(serverAddress);
                
                var networkStopwatch = Stopwatch.StartNew();
                var response = client.MinimaxSearch(request);
                networkStopwatch.Stop();
                double networkTimeMs = PreciseTimer.GetElapsedMilliseconds(networkStopwatch);
                
                _performanceTracker.UpdateMetrics(serverAddress, networkTimeMs);
                
                totalStopwatch.Stop();
                double totalTimeMs = PreciseTimer.GetElapsedMilliseconds(totalStopwatch);
                
                long serverComputationTimeMs = response.ServerComputationTimeMs;
                double clientOverheadMs = totalTimeMs - conversionTimeMs - networkTimeMs;
                if (clientOverheadMs < 0) clientOverheadMs = 0;
                
                Console.WriteLine($"Server {serverAddress} - Total: {totalTimeMs:F2}ms, Network: {networkTimeMs:F2}ms, Client overhead: {clientOverheadMs:F2}ms");
                
                GameLogger.LogMinimaxOperation(
                    "SendBoardForEvaluation", 
                    serverAddress, 
                    depth, 
                    isMaximizing, 
                    (long)totalTimeMs, 
                    (long)conversionTimeMs, 
                    (long)networkTimeMs, 
                    serverComputationTimeMs, 
                    response.Score);
                
                return response.Score;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with server {serverAddress}: {ex.Message}");
                _performanceTracker.MarkServerUnavailable(serverAddress);
                
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
                
                return RetryWithAnotherServer(board, depth, isMaximizing, serverAddress);
            }
        }

        private int RetryWithAnotherServer(CheckersBoard board, int depth, bool isMaximizing, string failedServer)
        {
            var totalStopwatch = Stopwatch.StartNew();
            double totalTimeMs;
            string alternativeServer = _performanceTracker.GetBestServer();
            if (alternativeServer == failedServer || string.IsNullOrEmpty(alternativeServer))
            {
                totalStopwatch.Stop();
                totalTimeMs = PreciseTimer.GetElapsedMilliseconds(totalStopwatch);
                GameLogger.LogMinimaxOperation(
                    "RetryFailed", 
                    "NONE", 
                    depth, 
                    isMaximizing, 
                    (long)totalTimeMs, 
                    0, 
                    0, 
                    0, 
                    0);
                throw new Exception("No available servers to handle the request");
            }

            Console.WriteLine($"Retrying with server {alternativeServer}");
            
            totalStopwatch.Stop();
            totalTimeMs = PreciseTimer.GetElapsedMilliseconds(totalStopwatch);
            GameLogger.LogMinimaxOperation(
                "RetryWithServer", 
                alternativeServer, 
                depth, 
                isMaximizing, 
                (long)totalTimeMs, 
                0, 
                0, 
                0, 
                0);
                
            return SendBoardForEvaluation(board, depth, isMaximizing);
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

        public Dictionary<string, (int activeRequests, double avgResponseTime, bool isAvailable)> GetServerStatus()
        {
            return _performanceTracker.GetServerStatus();
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

    // Klasa pomocnicza dla precyzyjnych pomiarów z kodu 2
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
