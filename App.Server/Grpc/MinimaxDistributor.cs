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
            var totalStopwatch = Stopwatch.StartNew();
            
            int result = SendBoardForEvaluation(board, depth, isMaximizing);
            
            totalStopwatch.Stop();
            GameLogger.LogMinimaxOperation(
                "DistributeMinimaxSearch", 
                "ALL", 
                depth, 
                isMaximizing, 
                totalStopwatch.ElapsedMilliseconds, 
                0, 
                0, 
                0, 
                result);
            
            return result;
        }

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

            // Mierzenie czasu konwersji planszy
            var conversionStopwatch = Stopwatch.StartNew();
            var compressedBoard = ConvertBoardTo32Format(board);
            conversionStopwatch.Stop();
            long conversionTime = conversionStopwatch.ElapsedMilliseconds;
            
            request.Board.Add(compressedBoard[0]);
            request.Board.Add(compressedBoard[1]);
            request.Board.Add(compressedBoard[2]);

            try
            {
                _performanceTracker.StartRequest(serverAddress);
                
                // Mierzenie czasu komunikacji sieciowej
                var networkStopwatch = Stopwatch.StartNew();
                var response = client.MinimaxSearch(request);
                networkStopwatch.Stop();
                long networkTime = networkStopwatch.ElapsedMilliseconds;
                
                _performanceTracker.UpdateMetrics(serverAddress, networkTime);
                
                totalStopwatch.Stop();
                long totalTime = totalStopwatch.ElapsedMilliseconds;
                
                // Obliczenie czasu obliczeń (całkowity czas minus czas konwersji i komunikacji)
                long computationTime = totalTime - conversionTime - networkTime;
                if (computationTime < 0) computationTime = 0; // Na wszelki wypadek
                
                Console.WriteLine($"Server {serverAddress} - Total: {totalTime}ms, Network: {networkTime}ms, Computation: {computationTime}ms");
                
                GameLogger.LogMinimaxOperation(
                    "SendBoardForEvaluation", 
                    serverAddress, 
                    depth, 
                    isMaximizing, 
                    totalTime, 
                    conversionTime, 
                    networkTime, 
                    computationTime, 
                    response.Score);
                
                return response.Score;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with server {serverAddress}: {ex.Message}");
                _performanceTracker.MarkServerUnavailable(serverAddress);
                
                totalStopwatch.Stop();
                GameLogger.LogMinimaxOperation(
                    "ServerFailure", 
                    serverAddress, 
                    depth, 
                    isMaximizing, 
                    totalStopwatch.ElapsedMilliseconds, 
                    conversionTime, 
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
            
            string alternativeServer = _performanceTracker.GetBestServer();
            if (alternativeServer == failedServer || string.IsNullOrEmpty(alternativeServer))
            {
                totalStopwatch.Stop();
                GameLogger.LogMinimaxOperation(
                    "RetryFailed", 
                    "NONE", 
                    depth, 
                    isMaximizing, 
                    totalStopwatch.ElapsedMilliseconds, 
                    0, 
                    0, 
                    0, 
                    0);
                throw new Exception("No available servers to handle the request");
            }

            Console.WriteLine($"Retrying with server {alternativeServer}");
            
            totalStopwatch.Stop();
            GameLogger.LogMinimaxOperation(
                "RetryWithServer", 
                alternativeServer, 
                depth, 
                isMaximizing, 
                totalStopwatch.ElapsedMilliseconds, 
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
}