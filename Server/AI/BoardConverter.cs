using System;

namespace MinimaxServer
{
    public static class BoardConverter
    {
        public static CheckersBoard ConvertFrom32Format(uint[] compressedBoard)
        {
            var board = new CheckersBoard();
            
            if (compressedBoard.Length < 3)
            {
               // Console.WriteLine("Invalid compressed board - not enough data");
                return board;
            }

            int fieldIndex = 0;

            // Konwertuj z formatu 32-polowego na szachownicę 8x8
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        if (fieldIndex >= 32) break;

                        PieceType piece = GetPieceFromCompressedBoard(compressedBoard, fieldIndex);
                        board.SetPiece(row, col, piece);
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

            // 8 pól na uint (4 bity każde)
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
