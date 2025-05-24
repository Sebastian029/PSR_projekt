using App.Grpc;
using App.Server;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace App.Client
{
    public class MinimaxDistributor
    {
        private readonly List<string> _serverAddresses;
        private readonly Dictionary<string, GrpcChannel> _channels;
        private int _currentServerIndex = 0;

        public MinimaxDistributor(List<string> serverAddresses)
        {
            _serverAddresses = serverAddresses;
            _channels = new Dictionary<string, GrpcChannel>();
            
            foreach (var address in _serverAddresses)
            {
                _channels[address] = GrpcChannel.ForAddress(address);
            }
        }

        public int DistributeMinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
        {
            return SendBoardForEvaluation(board, depth, isMaximizing);
        }

        private int SendBoardForEvaluation(CheckersBoard board, int depth, bool isMaximizing)
        {
            string serverAddress = GetNextServerAddress();
            var channel = _channels[serverAddress];
            var client = new CheckersEvaluationService.CheckersEvaluationServiceClient(channel);
            
            var request = new MinimaxRequest
            {
                Depth = depth,
                IsMaximizing = isMaximizing,
                RequestTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
            };

            // Konwertuj z 8x8 CheckersBoard na format 32-polowy dla serwera
            var compressedBoard = ConvertBoardTo32Format(board);
            request.Board.Add(compressedBoard[0]);
            request.Board.Add(compressedBoard[1]);
            request.Board.Add(compressedBoard[2]);

            var response = client.MinimaxSearch(request);
            var currentTime = DateTimeOffset.Now;
            var responseTime = response.ResponseTime.ToDateTimeOffset();
            Console.WriteLine($"Response time: {(currentTime - responseTime).TotalMilliseconds} ms");
            
            return response.Score;
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

        private string GetNextServerAddress()
        {
            lock (this)
            {
                string address = _serverAddresses[_currentServerIndex];
                _currentServerIndex = (_currentServerIndex + 1) % _serverAddresses.Count;
                return address;
            }
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
