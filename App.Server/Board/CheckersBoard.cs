using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public uint[] board;

        public CheckersBoard()
        {
            board = new uint[3];
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            for (int i = 0; i < 12; i++)
                SetField(i, (byte)PieceType.BlackPawn);
            for (int i = 12; i < 20; i++)
                SetField(i, (byte)PieceType.Empty);
            for (int i = 20; i < 32; i++)
                SetField(i, (byte)PieceType.WhitePawn);
        }

        public void ResetBoard()
        {
            for (int i = 0; i < 12; i++)
                SetField(i, (byte)PieceType.BlackPawn);
            for (int i = 12; i < 20; i++)
                SetField(i, (byte)PieceType.Empty);
            for (int i = 20; i < 32; i++)
                SetField(i, (byte)PieceType.WhitePawn);
        }

        public CheckersBoard Clone()
        {
            CheckersBoard clonedBoard = new CheckersBoard();
        
            clonedBoard.board = new uint[3];
            for (int i = 0; i < board.Length; i++)
            {
                clonedBoard.board[i] = board[i];
            }

            return clonedBoard;
        }

        public string SerializeBoard()
        {
            var boardState = new object[32];

            for (int i = 0; i < 32; i++)
            {
                PieceType pieceType = (PieceType)GetField(i);

                switch (pieceType)
                {
                    case PieceType.Empty:
                        boardState[i] = "empty";
                        break;
                    case PieceType.WhitePawn:
                        boardState[i] = "white";
                        break;
                    case PieceType.WhiteKing:
                        boardState[i] = "whiteKing";
                        break;
                    case PieceType.BlackPawn:
                        boardState[i] = "black";
                        break;
                    case PieceType.BlackKing:
                        boardState[i] = "blackKing";
                        break;
                    default:
                        boardState[i] = "empty";
                        break;
                }
            }

            return JsonSerializer.Serialize(boardState);
        }
    }
}