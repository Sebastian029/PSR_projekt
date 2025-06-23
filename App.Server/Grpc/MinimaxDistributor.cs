using App.Grpc;
using App.Server;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using System.Diagnostics;
using System.Linq;

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
            
            var channelOptions = new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(60),
                    KeepAlivePingDelay = TimeSpan.FromSeconds(600),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(300),
                    EnableMultipleHttp2Connections = true
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
            if (allTasks.Count == 0)
                return new List<int>();

            var results = new int[allTasks.Count];
            var tasks = new List<Task<(int index, int score)>>();

            // Distribute tasks across servers
            for (int i = 0; i < allTasks.Count; i++)
            {
                var serverAddress = GetNextServerAddress();
                var task = allTasks[i];
                tasks.Add(ProcessTaskOnServer(task.board, task.depth, task.isMaximizing, serverAddress, i));
            }

            // Wait for all tasks to complete
            var completedTasks = await Task.WhenAll(tasks);
            
            // Store results in correct order
            foreach (var (index, score) in completedTasks)
            {
                results[index] = score;
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

        private async Task<(int index, int score)> ProcessTaskOnServer(
            CheckersBoard board, int depth, bool isMaximizing, string serverAddress, int taskIndex)
        {
            var stopwatch = Stopwatch.StartNew();
            var conversionStopwatch = Stopwatch.StartNew();
            var channel = _channels[serverAddress];
            var client = new CheckersEvaluationService.CheckersEvaluationServiceClient(channel);
            
            var request = new MinimaxRequest
            {
                Depth = depth,
                IsMaximizing = isMaximizing,
                RequestTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
            };

            var compressedBoard = ConvertBoardTo32Format(board);
            request.Board.Add(compressedBoard[0]);
            request.Board.Add(compressedBoard[1]);
            request.Board.Add(compressedBoard[2]);
            conversionStopwatch.Stop();

            try
            {
                var networkStopwatch = Stopwatch.StartNew();
                var response = await client.MinimaxSearchAsync(request);
                networkStopwatch.Stop();
                stopwatch.Stop();
                
                Console.WriteLine($"Server {serverAddress} - Task {taskIndex} completed in {stopwatch.ElapsedMilliseconds}ms with score {response.Score}");
                
                // Log the minimax operation
                GameLogger.LogMinimaxOperation(
                    "DISTRIBUTED",  // operation
                    serverAddress,  // server
                    depth,         // depth
                    isMaximizing,  // isMaximizing
                    stopwatch.ElapsedMilliseconds,  // totalTimeMs
                    conversionStopwatch.ElapsedMilliseconds,  // conversionTimeMs
                    networkStopwatch.ElapsedMilliseconds,  // networkTimeMs
                    response.ServerComputationTimeMs,  // computationTimeMs
                    response.Score  // result
                );
                
                return (taskIndex, response.Score);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"Error with server {serverAddress} on task {taskIndex}: {ex.Message}");
                throw;
            }
        }

        private uint[] ConvertBoardTo32Format(CheckersBoard board)
        {
            uint[] result = new uint[3];
            int fieldIndex = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        if (fieldIndex >= 32) break;

                        PieceType piece = board.GetPiece(row, col);
                        byte pieceValue = ConvertPieceTypeToByte(piece);
                        
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

        public void Dispose()
        {
            foreach (var channel in _channels.Values)
            {
                channel?.Dispose();
            }
            _channels.Clear();
        }
    }
}
