using System;
using App.Server;

namespace MinimaxServer
{
    public static class BoardConverter
    {
        public static CheckersBoard ConvertFrom32Format(uint[] compressedBoard)
        {
            var board = new CheckersBoard();
            
            if (compressedBoard.Length < 3)
            {
                return board;
            }

            int fieldIndex = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        if (fieldIndex >= 32) break;

                        PieceType piece = GetPieceFromCompressedBoard(compressedBoard, fieldIndex);
                        board.SetPiece(row, col, (App.Server.PieceType)piece);
                        fieldIndex++;
                    }
                }
            }

            Console.WriteLine($"Converted {fieldIndex} fields from compressed board");
            return board;
        }

        private static PieceType GetPieceFromCompressedBoard(uint[] board, int index)
        {
            if (index < 0 || index >= 32) return PieceType.Empty;

            int arrayIndex = index / 8;
            int bitPosition = (index % 8) * 4;

            if (arrayIndex >= 3) return PieceType.Empty;

            byte value = (byte)((board[arrayIndex] >> bitPosition) & 0xF);
            return ConvertByteToPieceType(value);
        }

        private static PieceType ConvertByteToPieceType(byte value)
        {
            return value switch
            {
                0 => PieceType.Empty,
                1 => PieceType.WhitePawn,
                2 => PieceType.WhiteKing,
                3 => PieceType.BlackPawn,
                4 => PieceType.BlackKing,
                _ => PieceType.Empty
            };
        }

        private static bool IsDarkSquare(int row, int col)
        {
            return (row + col) % 2 == 1;
        }
    }
}
