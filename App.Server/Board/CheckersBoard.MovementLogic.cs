using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public void MovePiece(int from, int to)
        {
            byte piece = GetField(from);
            SetField(from, (byte)PieceType.Empty);
            SetField(to, piece);

            // Check if a capture was made
            if (Math.Abs(to - from) > 5) // Capture (jump of more than 5 squares)
            {
                int middleIndex = GetMiddleIndex(from, to);
                SetField(middleIndex, (byte)PieceType.Empty);
            }

            // Promote to king
            if (piece == (byte)PieceType.WhitePawn && to < 4)
            {
                SetField(to, (byte)PieceType.WhiteKing);
            }
            else if (piece == (byte)PieceType.BlackPawn && to >= 28)
            {
                SetField(to, (byte)PieceType.BlackKing);
            }
        }

        private bool IsSameColor(byte piece1, byte piece2)
        {
            return ((piece1 == (byte)PieceType.WhitePawn || piece1 == (byte)PieceType.WhiteKing) &&
                    (piece2 == (byte)PieceType.WhitePawn || piece2 == (byte)PieceType.WhiteKing)) ||
                   ((piece1 == (byte)PieceType.BlackPawn || piece1 == (byte)PieceType.BlackKing) &&
                    (piece2 == (byte)PieceType.BlackPawn || piece2 == (byte)PieceType.BlackKing));
        }

        public int GetMiddleIndex(int from, int to)
        {
            int row = from / 4;
            int check = row % 2;

            int middleIndex = check == 0
                ? (int)Math.Floor((double)(to + from + 1) / 2)
                : (int)Math.Floor((double)(to + from) / 2);

            return middleIndex;
        }
    }
}