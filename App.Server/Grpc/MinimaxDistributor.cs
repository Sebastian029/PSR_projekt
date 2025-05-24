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

            // Konwersja szachownicy 8x8 na tablicę 3 uint
            var compressedBoard = ConvertBoardTo3Uint(board);
            request.Board.Add(compressedBoard[0]);
            request.Board.Add(compressedBoard[1]);
            request.Board.Add(compressedBoard[2]);

            var response = client.MinimaxSearch(request);
            var currentTime = DateTimeOffset.Now;
            var responseTime = response.ResponseTime.ToDateTimeOffset();
            Console.WriteLine($"Response time: {(currentTime - responseTime).TotalMilliseconds} ms");
            
            return response.Score;
        }

        private uint[] ConvertBoardTo3Uint(CheckersBoard board)
        {
            uint[] compressedBoard = new uint[3];
            int fieldIndex = 0;

            // Iteruj przez szachownicę 8x8 i pakuj tylko ciemne pola
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        PieceType piece = board.GetPiece(row, col);
                        byte pieceValue = ConvertPieceTypeToByte(piece);
                        
                        SetFieldInCompressedBoard(compressedBoard, fieldIndex, pieceValue);
                        fieldIndex++;
                    }
                }
            }

            return compressedBoard;
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

        private void SetFieldInCompressedBoard(uint[] board, int index, byte value)
        {
            if (index < 0 || index >= 32) return; // 32 ciemne pola na szachownicy

            int arrayIndex = index / 10; // 10 pól na uint (3 bity każde + 2 bity zapasu)
            int bitPosition = (index % 10) * 3; // 3 bity na pole

            // Wyczyść poprzednią wartość
            uint mask = ~((uint)0x7 << bitPosition);
            board[arrayIndex] &= mask;

            // Ustaw nową wartość
            board[arrayIndex] |= ((uint)value & 0x7) << bitPosition;
        }

        // Metoda pomocnicza do odczytu pola (do testowania)
        private byte GetFieldFromCompressedBoard(uint[] board, int index)
        {
            if (index < 0 || index >= 32) return 0;

            int arrayIndex = index / 10;
            int bitPosition = (index % 10) * 3;

            return (byte)((board[arrayIndex] >> bitPosition) & 0x7);
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

        // Metoda testowa do weryfikacji konwersji
        public void TestConversion(CheckersBoard board)
        {
            var compressed = ConvertBoardTo3Uint(board);
            Console.WriteLine($"Compressed board: [{compressed[0]}, {compressed[1]}, {compressed[2]}]");
            
            // Weryfikacja - odczytaj i porównaj
            int fieldIndex = 0;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        PieceType originalPiece = board.GetPiece(row, col);
                        byte compressedValue = GetFieldFromCompressedBoard(compressed, fieldIndex);
                        byte expectedValue = ConvertPieceTypeToByte(originalPiece);
                        
                        if (compressedValue != expectedValue)
                        {
                            Console.WriteLine($"Mismatch at field {fieldIndex} ({row},{col}): expected {expectedValue}, got {compressedValue}");
                        }
                        fieldIndex++;
                    }
                }
            }
        }
    }
}
