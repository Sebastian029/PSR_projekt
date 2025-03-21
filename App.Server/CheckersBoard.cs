using System;
using System.Collections.Generic;
using App.Server;
using System.Text.Json;

public class CheckersBoard
{
    private uint[] board; // 10 x uint = 320 bitów (32 pola x 10 bitów)

    public CheckersBoard()
    {
        board = new uint[10]; // Każde pole ma teraz 10 bitów, łatwiejsze operacje
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        for (int i = 0; i < 12; i++) // Pierwsze 12 pól dla czarnych pionków
            SetField(i, (byte)PieceType.BlackPawn);
        for (int i = 12; i < 20; i++) // Środkowe 8 pól puste
            SetField(i, (byte)PieceType.Empty);
        for (int i = 20; i < 32; i++) // Ostatnie 12 pól dla białych pionków
            SetField(i, (byte)PieceType.WhitePawn);
    }

    public byte GetField(int index)
    {
        int bitPosition = index * 10;
        int arrayIndex = bitPosition / 32;
        int bitOffset = bitPosition % 32;

        uint mask = (uint)(0b1111111111 << bitOffset); // 10-bit maska
        return (byte)((board[arrayIndex] & mask) >> bitOffset);
    }

    public void SetField(int index, byte value)
    {
        int bitPosition = index * 10;
        int arrayIndex = bitPosition / 32;
        int bitOffset = bitPosition % 32;

        uint mask = (uint)(0b1111111111 << bitOffset); // 10-bit maska
        board[arrayIndex] &= ~mask; // Zerowanie pola
        board[arrayIndex] |= (uint)(value << bitOffset); // Ustawienie nowej wartości
    }

    public List<int> GetValidMoves(int index)
    {
        List<int> moves = new List<int>();
        byte piece = GetField(index);
        if (piece == (byte)PieceType.Empty) return moves;

        int[] directions = piece == (byte)PieceType.WhitePawn ? new int[] { -4, -3 } :
                           piece == (byte)PieceType.BlackPawn ? new int[] { 4, 3 } :
                           new int[] { -4, -3, 4, 3 }; // Damka

        foreach (var dir in directions)
        {
            int targetIndex = index + dir;
            if (targetIndex >= 0 && targetIndex < 32 && GetField(targetIndex) == (byte)PieceType.Empty)
                moves.Add(targetIndex);
        }

        return moves;
    }

    public List<int> GetValidCaptures(int index)
    {
        List<int> captures = new List<int>();
        byte piece = GetField(index);
        if (piece == (byte)PieceType.Empty) return captures;

        int[] captureDirections = piece == (byte)PieceType.WhitePawn ? new int[] { -8, -6 } :
                                  piece == (byte)PieceType.BlackPawn ? new int[] { 8, 6 } :
                                  new int[] { -8, -6, 8, 6 };

        foreach (var dir in captureDirections)
        {
            int middleIndex = index + dir / 2;
            int targetIndex = index + dir;
            if (targetIndex >= 0 && targetIndex < 32 && GetField(targetIndex) == (byte)PieceType.Empty)
            {
                byte capturedPiece = GetField(middleIndex);
                if (capturedPiece != (byte)PieceType.Empty && capturedPiece != piece)
                    captures.Add(targetIndex);
            }
        }

        return captures;
    }

    public void MovePiece(int from, int to)
    {
        byte piece = GetField(from);
        SetField(from, (byte)PieceType.Empty);
        SetField(to, piece);

        // Jeśli ruch to bicie, usuń przeciwnika
        if (Math.Abs(to - from) > 4) // Bicie przesuwa o więcej niż 4 pola
        {
            int middle = (from + to) / 2;
            SetField(middle, (byte)PieceType.Empty);
        }

        // Awansowanie pionka na damkę
        if (piece == (byte)PieceType.WhitePawn && to >= 28) SetField(to, (byte)PieceType.WhiteKing);
        if (piece == (byte)PieceType.BlackPawn && to < 4) SetField(to, (byte)PieceType.BlackKing);
    }
    public string SerializeBoard()
    {
        var boardState = new object[32]; // Use an array of objects to represent each square on the board

        for (int i = 0; i < 32; i++)
        {
            PieceType pieceType = (PieceType)GetField(i); // Get the piece type at the current field (as an enum)

            // Assign a more human-readable representation of the board state to each square
            switch (pieceType)
            {
                case PieceType.Empty:
                    boardState[i] = "empty"; // No piece on the square
                    break;
                case PieceType.WhitePawn:
                    boardState[i] = "white"; // White pawn
                    break;
                case PieceType.WhiteKing:
                    boardState[i] = "white-king"; // White king
                    break;
                case PieceType.BlackPawn:
                    boardState[i] = "black"; // Black pawn
                    break;
                case PieceType.BlackKing:
                    boardState[i] = "black-king"; // Black king
                    break;
                default:
                    boardState[i] = "empty"; // Fallback if something goes wrong
                    break;
            }
        }

        // Convert the board state to JSON for easy transmission
        return JsonSerializer.Serialize(boardState); // Convert the boardState array to a JSON string
    }


    public void PrintBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if ((row + col) % 2 == 0)
                {
                    Console.Write(" . ");
                }
                else
                {
                    int index32 = (row / 2) * 4 + (col / 2);
                    byte piece = GetField(index32);
                    char symbol = piece == (byte)PieceType.WhitePawn ? 'w' :
                                  piece == (byte)PieceType.BlackPawn ? 'b' :
                                  piece == (byte)PieceType.WhiteKing ? 'W' :
                                  piece == (byte)PieceType.BlackKing ? 'B' : '-';
                    Console.Write($" {symbol} ");
                }
            }
            Console.WriteLine();
        }
    }
}
