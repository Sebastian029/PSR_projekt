// CheckersBoard.MovementLogic.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            PieceType piece = GetPiece(fromRow, fromCol);
            SetPiece(fromRow, fromCol, PieceType.Empty);
            SetPiece(toRow, toCol, piece);

            // Sprawdź czy nastąpiło bicie
            if (Math.Abs(toRow - fromRow) == 2)
            {
                int middleRow = (fromRow + toRow) / 2;
                int middleCol = (fromCol + toCol) / 2;
                SetPiece(middleRow, middleCol, PieceType.Empty);
            }

            // Promuj do damki
            if (piece == PieceType.WhitePawn && toRow == 0)
            {
                SetPiece(toRow, toCol, PieceType.WhiteKing);
            }
            else if (piece == PieceType.BlackPawn && toRow == 7)
            {
                SetPiece(toRow, toCol, PieceType.BlackKing);
            }
        }

        public void MovePiece(object from, object to)
        {
            // Kompatybilność z oryginalnym interfejsem
            if (from is (int fromRow, int fromCol) && to is (int toRow, int toCol))
            {
                MovePiece(fromRow, fromCol, toRow, toCol);
            }
        }

        private bool IsSameColor(PieceType piece1, PieceType piece2)
        {
            return ((piece1 == PieceType.WhitePawn || piece1 == PieceType.WhiteKing) &&
                    (piece2 == PieceType.WhitePawn || piece2 == PieceType.WhiteKing)) ||
                   ((piece1 == PieceType.BlackPawn || piece1 == PieceType.BlackKing) &&
                    (piece2 == PieceType.BlackPawn || piece2 == PieceType.BlackKing));
        }
    }
}