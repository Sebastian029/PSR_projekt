using System;
using System.Collections.Generic;
using App.Server;
using System.Text.Json;

public class CheckersBoard
{
    private uint[] board; // 96 bitów (3 bity na pole, 32 pola)

    public CheckersBoard()
    {
        board = new uint[3]; // 3 uinty = 96 bitów
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
        int bitPosition = index * 3; // 3 bity na pole
        int arrayIndex = bitPosition / 32; // Określenie, w którym `uint` znajduje się pole
        int bitOffset = bitPosition % 32; // Określenie przesunięcia bitu w obrębie tego `uint`

        uint mask = (uint)(0b111 << bitOffset); // 3-bitowa maska
        return (byte)((board[arrayIndex] & mask) >> bitOffset);
    }

    public void SetField(int index, byte value)
    {
        int bitPosition = index * 3; // 3 bity na pole
        int arrayIndex = bitPosition / 32; // Określenie, w którym `uint` znajduje się pole
        int bitOffset = bitPosition % 32; // Określenie przesunięcia bitu w obrębie tego `uint`

        uint mask = (uint)(0b111 << bitOffset); // 3-bitowa maska
        board[arrayIndex] &= ~mask; // Zerowanie bitów w danym miejscu
        board[arrayIndex] |= (uint)(value << bitOffset); // Ustawienie nowej wartości na pole
    }

    public List<int> GetValidMoves(int index)
    {
        List<int> moves = new List<int>();
        byte piece = GetField(index);
        if (piece == (byte)PieceType.Empty) return moves;

        int row = index / 4;
        int col = index % 4;

        bool isEvenRow = (row % 2 == 0);

        List<int> offsets = new List<int>();

        if (piece == (byte)PieceType.WhitePawn)
        {
            offsets.Add(isEvenRow ? -4 : -3); // Up-left
            offsets.Add(isEvenRow ? -3 : -4); // Up-right
        }
        else if (piece == (byte)PieceType.BlackPawn)
        {
            offsets.Add(isEvenRow ? 4 : 5);  // Down-left
            offsets.Add(isEvenRow ? 5 : 4);  // Down-right
        }
        else
        {
            offsets.Add(isEvenRow ? -4 : -3); // Up-left
            offsets.Add(isEvenRow ? -3 : -4); // Up-right
            offsets.Add(isEvenRow ? 4 : 5);  // Down-left
            offsets.Add(isEvenRow ? 5 : 4);  // Down-right
        }

        foreach (int offset in offsets)
        {
            int targetIndex = index + offset;

            if (targetIndex >= 0 && targetIndex < 32)
            {
                int targetRow = targetIndex / 4;
                int targetCol = targetIndex % 4;

                if (Math.Abs(targetRow - row) == 1)
                {
                    if (GetField(targetIndex) == (byte)PieceType.Empty)
                    {
                        moves.Add(targetIndex);
                    }
                }
            }
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

        if (Math.Abs(to - from) > 4)
        {
            int middle = (from + to) / 2;
            SetField(middle, (byte)PieceType.Empty);
        }

        if (piece == (byte)PieceType.WhitePawn && to >= 28) SetField(to, (byte)PieceType.WhiteKing);
        if (piece == (byte)PieceType.BlackPawn && to < 4) SetField(to, (byte)PieceType.BlackKing);
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
                    boardState[i] = "white-king";
                    break;
                case PieceType.BlackPawn:
                    boardState[i] = "black";
                    break;
                case PieceType.BlackKing:
                    boardState[i] = "black-king";
                    break;
                default:
                    boardState[i] = "empty";
                    break;
            }
        }

        return JsonSerializer.Serialize(boardState);
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
