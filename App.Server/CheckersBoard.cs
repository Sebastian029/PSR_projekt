﻿using App.Server;
using System.Collections.Generic;
using System.Text.Json;

public class CheckersBoard
{
    private uint[] board;

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

    public byte GetField(int index)
    {
        int bitPosition = index * 3;
        int arrayIndex = bitPosition / 32;
        int bitOffset = bitPosition % 32;

        uint mask = (uint)(0b111 << bitOffset);
        return (byte)((board[arrayIndex] & mask) >> bitOffset);
    }

    public void SetField(int index, byte value)
    {
        int bitPosition = index * 3;
        int arrayIndex = bitPosition / 32;
        int bitOffset = bitPosition % 32;

        uint mask = (uint)(0b111 << bitOffset);
        board[arrayIndex] &= ~mask;
        board[arrayIndex] |= (uint)(value << bitOffset);
    }

    public List<int> GetValidMoves(int index)
    {
        List<int> moves = new List<int>();
        byte piece = GetField(index);
        if (piece == (byte)PieceType.Empty) return moves;

        int row = index / 4;
        bool isEvenRow = (row % 2 == 0);

        // Dla pionków (białych i czarnych)
        if (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.BlackPawn)
        {
            List<int> offsets = new List<int>();
            if (piece == (byte)PieceType.WhitePawn)
            {
                // Białe pionki mogą iść tylko w górę
                offsets.Add(isEvenRow ? -4 : -5); // Up-left
                offsets.Add(isEvenRow ? -3 : -4); // Up-right
            }
            else // BlackPawn
            {
                // Czarne pionki mogą iść tylko w dół
                offsets.Add(isEvenRow ? 4 : 3);  // Down-left
                offsets.Add(isEvenRow ? 5 : 4);  // Down-right
            }

            foreach (int offset in offsets)
            {
                int targetIndex = index + offset;
                if (targetIndex >= 0 && targetIndex < 32 && GetField(targetIndex) == (byte)PieceType.Empty)
                {
                    int targetRow = targetIndex / 4;
                    if (Math.Abs(targetRow - row) == 1)
                    {
                        // Sprawdź czy ruch nie wychodzi poza planszę (dla skrajnych kolumn)
                        int col = index % 4;
                        if ((col == 0 && offset == -5) || (col == 3 && offset == -3)) continue;
                        if ((col == 0 && offset == 3) || (col == 3 && offset == 5)) continue;

                        moves.Add(targetIndex);
                    }
                }
            }
        }
        else // Dla dam (królów)
        {
            // Kierunki ruchu damki
            int[] kingDirections = isEvenRow
                ? new int[] { -4, -3, 4, 5 }    // Even row directions
                : new int[] { -5, -4, 3, 4 };   // Odd row directions

            foreach (int dir in kingDirections)
            {
                int targetIndex = index + dir;
                if (targetIndex >= 0 && targetIndex < 32 && GetField(targetIndex) == (byte)PieceType.Empty)
                {
                    int targetRow = targetIndex / 4;
                    if (Math.Abs(targetRow - row) == 1)
                    {
                        // Sprawdź czy ruch nie wychodzi poza planszę (dla skrajnych kolumn)
                        int col = index % 4;
                        if ((col == 0 && (dir == -5 || dir == 3)) || (col == 3 && (dir == -3 || dir == 5))) continue;

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

        int row = index / 4;
        bool isEvenRow = (row % 2 == 0);

        // Dla wszystkich pionków (białych, czarnych i dam)
        int[] captureDirections = isEvenRow
            ? new int[] { -7, -9, 7, 9 }
            : new int[] { -9, -7, 9, 7 };

        foreach (var dir in captureDirections)
        {
            int targetIndex = index + dir;
            if (targetIndex >= 0 && targetIndex < 32)
            {
                // Sprawdź czy cel jest pusty
                if (GetField(targetIndex) == (byte)PieceType.Empty)
                {
                    int middleIndex = GetMiddleIndex(index, targetIndex);
                    if (middleIndex >= 0 && middleIndex < 32)
                    {
                        byte capturedPiece = GetField(middleIndex);
                        if (capturedPiece != (byte)PieceType.Empty && !IsSameColor(piece, capturedPiece))
                        {
                            // Sprawdź czy ruch nie wychodzi poza planszę
                            int col = index % 4;
                            if ((col == 0 && (dir == -9 || dir == 7)) || (col == 3 && (dir == -7 || dir == 9))) continue;

                            // Usunięto ograniczenie kierunku dla pionków - mogą bić w obie strony
                            captures.Add(targetIndex);
                        }
                    }
                }
            }
        }

        return captures;
    }

    public List<int> GetMultipleCaptures(int index, List<int> previousCaptures = null)
    {
        List<int> multipleCaptures = new List<int>();
        byte piece = GetField(index);
        if (piece == (byte)PieceType.Empty) return multipleCaptures;

        previousCaptures = previousCaptures ?? new List<int>();

        var initialCaptures = GetValidCaptures(index);

        foreach (var capture in initialCaptures)
        {
            // Symuluj ruch
            byte originalPiece = GetField(index);
            SetField(index, (byte)PieceType.Empty);
            int middleIndex = GetMiddleIndex(index, capture);
            byte capturedPiece = GetField(middleIndex);
            SetField(middleIndex, (byte)PieceType.Empty);
            SetField(capture, originalPiece);

            // Sprawdź czy pionek powinien zostać damką
            byte currentPiece = originalPiece;
            if (originalPiece == (byte)PieceType.WhitePawn && capture < 4)
            {
                SetField(capture, (byte)PieceType.WhiteKing);
                currentPiece = (byte)PieceType.WhiteKing;
            }
            else if (originalPiece == (byte)PieceType.BlackPawn && capture >= 28)
            {
                SetField(capture, (byte)PieceType.BlackKing);
                currentPiece = (byte)PieceType.BlackKing;
            }

            // Sprawdź kolejne bicia
            var furtherCaptures = GetValidCaptures(capture);
            if (furtherCaptures.Count > 0)
            {
                foreach (var furtherCapture in furtherCaptures)
                {
                    List<int> sequence = new List<int>(previousCaptures) { capture, furtherCapture };
                    multipleCaptures.AddRange(sequence);

                    // Rekurencyjnie sprawdź dalsze bicia
                    var deeperCaptures = GetMultipleCaptures(furtherCapture, sequence);
                    multipleCaptures.AddRange(deeperCaptures);
                }
            }
            else
            {
                multipleCaptures.Add(capture);
            }

            // Cofnij symulację
            SetField(capture, (byte)PieceType.Empty);
            SetField(middleIndex, capturedPiece);
            SetField(index, originalPiece);
        }

        return multipleCaptures.Distinct().ToList();
    }

    public void MovePiece(int from, int to)
    {
        byte piece = GetField(from);
        SetField(from, (byte)PieceType.Empty);
        SetField(to, piece);

        // Sprawdź czy wykonano bicie
        if (Math.Abs(to - from) > 5) // Bicie (przeskok o więcej niż 5 pól)
        {
            int middleIndex = GetMiddleIndex(from, to);
            SetField(middleIndex, (byte)PieceType.Empty);
        }

        // Awans na damkę
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