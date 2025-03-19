using System;

namespace App.Server
{
    public class Checkers
    {
        private const int BoardSize = 8;
        private char[,] board;
        private bool isWhiteTurn = true;

        public Checkers()
        {
            board = new char[BoardSize, BoardSize];
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    if ((i + j) % 2 == 1)
                    {
                        if (i < 3)
                            board[i, j] = 'W'; 
                        else if (i > 4)
                            board[i, j] = 'B'; 
                        else
                            board[i, j] = '.'; 
                    }
                    else
                    {
                        board[i, j] = ' '; 
                    }
                }
            }
        }

        public void PrintBoard()
        {
            Console.WriteLine("  0 1 2 3 4 5 6 7");
            for (int i = 0; i < BoardSize; i++)
            {
                Console.Write(i + " ");
                for (int j = 0; j < BoardSize; j++)
                {
                    Console.Write(board[i, j] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("Current turn: " + (isWhiteTurn ? "White (W)" : "Black (B)"));
        }

        public bool MovePiece(int fromX, int fromY, int toX, int toY)
        {
            if (!IsValidMove(fromX, fromY, toX, toY, out int capturedX, out int capturedY))
            {
                Console.WriteLine("Invalid move.");
                return false;
            }

            board[toX, toY] = board[fromX, fromY];
            board[fromX, fromY] = '.';

            if (capturedX != -1 && capturedY != -1)
            {
                board[capturedX, capturedY] = '.';
            }

            isWhiteTurn = !isWhiteTurn;
            return true;
        }

        private bool IsValidMove(int fromX, int fromY, int toX, int toY, out int capturedX, out int capturedY)
        {
            capturedX = -1;
            capturedY = -1;

            if (toX < 0 || toX >= BoardSize || toY < 0 || toY >= BoardSize)
                return false;
            if (board[toX, toY] != '.')
                return false;

            char playerPiece = board[fromX, fromY];
            char opponentPiece = (playerPiece == 'W') ? 'B' : 'W';

            if (Math.Abs(toX - fromX) == 1 && Math.Abs(toY - fromY) == 1)
            {
                if ((playerPiece == 'W' && isWhiteTurn && toX == fromX + 1) ||
                    (playerPiece == 'B' && !isWhiteTurn && toX == fromX - 1))
                {
                    return true;
                }
            }

            if (Math.Abs(toX - fromX) == 2 && Math.Abs(toY - fromY) == 2)
            {
                capturedX = (fromX + toX) / 2;
                capturedY = (fromY + toY) / 2;

                if (board[capturedX, capturedY] == opponentPiece)
                {
                    if ((playerPiece == 'W' && isWhiteTurn && toX == fromX + 2) ||
                        (playerPiece == 'B' && !isWhiteTurn && toX == fromX - 2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
