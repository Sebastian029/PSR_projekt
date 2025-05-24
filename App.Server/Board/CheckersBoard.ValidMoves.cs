// CheckersBoard.ValidMoves.cs
using System;
using System.Collections.Generic;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public List<(int row, int col)> GetValidMoves(int row, int col)
        {
            List<(int, int)> moves = new List<(int, int)>();
            PieceType piece = GetPiece(row, col);
            
            if (piece == PieceType.Empty || !IsDarkSquare(row, col)) 
                return moves;

            if (piece == PieceType.WhitePawn)
            {
                // Białe pionki poruszają się w górę (zmniejszenie row)
                AddPawnMoves(moves, row, col, -1);
            }
            else if (piece == PieceType.BlackPawn)
            {
                // Czarne pionki poruszają się w dół (zwiększenie row)
                AddPawnMoves(moves, row, col, 1);
            }
            else if (piece == PieceType.WhiteKing || piece == PieceType.BlackKing)
            {
                // Damki mogą poruszać się w obu kierunkach
                AddKingMoves(moves, row, col);
            }

            return moves;
        }

        private void AddPawnMoves(List<(int, int)> moves, int row, int col, int direction)
        {
            // Sprawdź ruchy po przekątnej
            int[] colOffsets = { -1, 1 };
            
            foreach (int colOffset in colOffsets)
            {
                int newRow = row + direction;
                int newCol = col + colOffset;
                
                if (IsValidPosition(newRow, newCol) && IsDarkSquare(newRow, newCol) && IsEmpty(newRow, newCol))
                {
                    moves.Add((newRow, newCol));
                }
            }
        }

        private void AddKingMoves(List<(int, int)> moves, int row, int col)
        {
            // Damki mogą poruszać się we wszystkich 4 kierunkach przekątnych
            int[] directions = { -1, 1 };
            
            foreach (int rowDir in directions)
            {
                foreach (int colDir in directions)
                {
                    // Sprawdź wszystkie pola w danym kierunku
                    for (int i = 1; i < BOARD_SIZE; i++)
                    {
                        int newRow = row + (i * rowDir);
                        int newCol = col + (i * colDir);
                        
                        if (!IsValidPosition(newRow, newCol) || !IsDarkSquare(newRow, newCol))
                            break;
                            
                        if (IsEmpty(newRow, newCol))
                        {
                            moves.Add((newRow, newCol));
                        }
                        else
                        {
                            break; // Zatrzymaj się przy napotkaniu figury
                        }
                    }
                }
            }
        }
    }
}
