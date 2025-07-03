using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public PieceType[,] board;
        private const int BOARD_SIZE = 8;

        public CheckersBoard()
        {
            board = new PieceType[BOARD_SIZE, BOARD_SIZE];
            InitializeBoard();
        }
        public bool IsDarkSquare(int row, int col)
        {
            return (row + col) % 2 == 1;
        }


        private void InitializeBoard()
        {
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    board[row, col] = PieceType.Empty;
                }
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        board[row, col] = PieceType.BlackPawn;
                    }
                }
            }

            for (int row = 5; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    if (IsDarkSquare(row, col))
                    {
                        board[row, col] = PieceType.WhitePawn;
                    }
                }
            }
        }

        public void ResetBoard()
        {
            InitializeBoard();
        }

        public CheckersBoard Clone()
        {
            try
            {
                CheckersBoard clonedBoard = new CheckersBoard();
        
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        clonedBoard.board[row, col] = this.board[row, col];
                    }
                }

                return clonedBoard;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public string SerializeBoard()
        {
            var boardState = new List<object>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!IsDarkSquare(row, col))
                    {
                        boardState.Add("invalid");
                        continue;
                    }

                    switch (board[row, col])
                    {
                        case PieceType.Empty:
                            boardState.Add("empty");
                            break;
                        case PieceType.WhitePawn:
                            boardState.Add("white");
                            break;
                        case PieceType.WhiteKing:
                            boardState.Add("whiteKing");
                            break;
                        case PieceType.BlackPawn:
                            boardState.Add("black");
                            break;
                        case PieceType.BlackKing:
                            boardState.Add("blackKing");
                            break;
                        default:
                            boardState.Add("empty");
                            break;
                    }
                }
            }
            return JsonSerializer.Serialize(boardState);
        }

        
        

        public PieceType GetPiece(int row, int col)
        {
            if (row < 0 || row >= 8 || col < 0 || col >= 8)
            {
                return PieceType.Empty;
            }
    
            return board[row, col];
        }

        public void SetPiece(int row, int col, PieceType piece)
        {
            if (row < 0 || row >= 8 || col < 0 || col >= 8)
            {
                return;
            }
    
            board[row, col] = piece;
        }


        public bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
        }

        public bool IsEmpty(int row, int col)
        {
            return IsValidPosition(row, col) && board[row, col] == PieceType.Empty;
        }

        public bool HasPiece(int row, int col, bool isWhite)
        {
            if (!IsValidPosition(row, col)) return false;
            
            PieceType piece = board[row, col];
            if (isWhite)
                return piece == PieceType.WhitePawn || piece == PieceType.WhiteKing;
            else
                return piece == PieceType.BlackPawn || piece == PieceType.BlackKing;
        }

        public string GetSquareNotation(int row, int col)
        {
            if (!IsValidPosition(row, col)) return "";
            return $"{(char)('a' + col)}{8 - row}";
        }

        public void PrintBoard()
        {
            Console.WriteLine("  a b c d e f g h");
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                Console.Write($"{8 - row} ");
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    if (!IsDarkSquare(row, col))
                    {
                        Console.Write("  ");
                        continue;
                    }

                    switch (board[row, col])
                    {
                        case PieceType.Empty:
                            Console.Write("· ");
                            break;
                        case PieceType.WhitePawn:
                            Console.Write("○ ");
                            break;
                        case PieceType.WhiteKing:
                            Console.Write("♔ ");
                            break;
                        case PieceType.BlackPawn:
                            Console.Write("● ");
                            break;
                        case PieceType.BlackKing:
                            Console.Write("♚ ");
                            break;
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
