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

        private void InitializeBoard()
        {
            // Wyczyść całą szachownicę
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    board[row, col] = PieceType.Empty;
                }
            }

            // Ustaw czarne pionki na pierwszych 3 rzędach (tylko ciemne pola)
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

            // Ustaw białe pionki na ostatnich 3 rzędach (tylko ciemne pola)
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
            CheckersBoard clonedBoard = new CheckersBoard();
            
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    clonedBoard.board[row, col] = board[row, col];
                }
            }

            return clonedBoard;
        }

        public string SerializeBoard()
        {
            var boardState = new object[BOARD_SIZE, BOARD_SIZE];

            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    if (!IsDarkSquare(row, col))
                    {
                        boardState[row, col] = "invalid";
                        continue;
                    }

                    switch (board[row, col])
                    {
                        case PieceType.Empty:
                            boardState[row, col] = "empty";
                            break;
                        case PieceType.WhitePawn:
                            boardState[row, col] = "white";
                            break;
                        case PieceType.WhiteKing:
                            boardState[row, col] = "whiteKing";
                            break;
                        case PieceType.BlackPawn:
                            boardState[row, col] = "black";
                            break;
                        case PieceType.BlackKing:
                            boardState[row, col] = "blackKing";
                            break;
                        default:
                            boardState[row, col] = "empty";
                            break;
                    }
                }
            }

            return JsonSerializer.Serialize(boardState);
        }

        private bool IsDarkSquare(int row, int col)
        {
            return (row + col) % 2 == 1;
        }

        public PieceType GetPiece(int row, int col)
        {
            if (row < 0 || row >= BOARD_SIZE || col < 0 || col >= BOARD_SIZE)
                return PieceType.Empty;
            
            return board[row, col];
        }

        public void SetPiece(int row, int col, PieceType piece)
        {
            if (row < 0 || row >= BOARD_SIZE || col < 0 || col >= BOARD_SIZE)
                return;
            
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

        // Konwertuje pozycję na notację algebraiczną
        public string GetSquareNotation(int row, int col)
        {
            if (!IsValidPosition(row, col)) return "";
            return $"{(char)('a' + col)}{8 - row}";
        }

        // Wyświetla szachownicę w konsoli (do debugowania)
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
