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

namespace App.Client
{
    public class MinimaxDistributor : IDisposable
    {
        private readonly List<string> _serverAddresses;
        private readonly Dictionary<string, GrpcChannel> _channels;
        private readonly ServerPerformanceTracker _performanceTracker;
        private readonly object _lockObject = new object();

        public MinimaxDistributor(List<string> serverAddresses)
        {
            _serverAddresses = serverAddresses;
            _channels = new Dictionary<string, GrpcChannel>();
            _performanceTracker = new ServerPerformanceTracker();
            
            foreach (var address in _serverAddresses)
            {
                _channels[address] = GrpcChannel.ForAddress(address);
                _performanceTracker.RegisterServer(address);
            }
        }

        public int DistributeMinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
        {
            // POPRAWKA: Usuń nakładający się pomiar - pomiar będzie w SendBoardForEvaluation
            int result = SendBoardForEvaluation(board, depth, isMaximizing);
            return result;
        }

        private int SendBoardForEvaluation(CheckersBoard board, int depth, bool isMaximizing)
        {
            // Sprawdź dostępność wysokiej rozdzielczości
            if (!Stopwatch.IsHighResolution)
            {
                Console.WriteLine("Warning: High-resolution timing not available");
            }

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

            // POPRAWKA: Użyj ElapsedTicks dla większej precyzji
            var conversionStopwatch = Stopwatch.StartNew();
            var compressedBoard = ConvertBoardTo32Format(board);
            conversionStopwatch.Stop();
            double conversionTimeMs = (double)conversionStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
            
            request.Board.Add(compressedBoard[0]);
            request.Board.Add(compressedBoard[1]);
            request.Board.Add(compressedBoard[2]);

            try
            {
                _performanceTracker.StartRequest(serverAddress);
                
                // POPRAWKA: Precyzyjny pomiar czasu komunikacji sieciowej
                var networkStopwatch = Stopwatch.StartNew();
                var response = client.MinimaxSearch(request);
                networkStopwatch.Stop();
                double networkTimeMs = (double)networkStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
                
                _performanceTracker.UpdateMetrics(serverAddress, networkTimeMs);
                
                totalStopwatch.Stop();
                double totalTimeMs = (double)totalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
                
                // POPRAWKA: Zmień interpretację - to jest client overhead, nie computation time
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
                    (long)serverComputationTimeMs, // Zmieniona nazwa z computationTime
                    response.Score);
                
                return response.Score;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with server {serverAddress}: {ex.Message}");
                _performanceTracker.MarkServerUnavailable(serverAddress);
                
                totalStopwatch.Stop();
                double totalTimeMs = (double)totalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
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
                
                // Try another server
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
                totalTimeMs = (double)totalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
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
            totalTimeMs = (double)totalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
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

            // Konwertuj z 8x8 na 32-polowy format
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        if (fieldIndex >= 32) break;

                        PieceType piece = board.GetPiece(row, col);
                        byte pieceValue = ConvertPieceTypeToByte(piece);
                        
                        // Użyj 4 bity na pole (kompatybilne z serwerem)
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
            // Zapisz końcowe podsumowanie przed zamknięciem
            Console.WriteLine("Saving final summary...");
            GameLogger.WriteMinimaxSummary();
            
            foreach (var channel in _channels.Values)
            {
                channel?.Dispose();
            }
            _channels.Clear();
        }
    }

    // DODATKOWA KLASA POMOCNICZA dla precyzyjnych pomiarów
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
