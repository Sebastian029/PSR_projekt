// CheckersBoard.MovementLogic.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Server
{
    public partial class CheckersBoard
    {
        // CheckersBoard.cs
public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
{
    // Walidacja współrzędnych
    if (fromRow < 0 || fromRow >= 8 || fromCol < 0 || fromCol >= 8 ||
        toRow < 0 || toRow >= 8 || toCol < 0 || toCol >= 8)
    {
        Console.WriteLine($"MovePiece: Invalid coordinates from ({fromRow},{fromCol}) to ({toRow},{toCol})");
        return;
    }

    PieceType piece = GetPiece(fromRow, fromCol);
    if (piece == PieceType.Empty)
    {
        Console.WriteLine($"MovePiece: No piece at ({fromRow},{fromCol})");
        return;
    }

    // Walidacja kierunku tylko dla pionków (nie dla królów)
    if (piece == PieceType.WhitePawn && toRow >= fromRow)
    {
        Console.WriteLine($"MovePiece: White pawn cannot move from row {fromRow} to row {toRow}");
        return;
    }
    if (piece == PieceType.BlackPawn && toRow <= fromRow)
    {
        Console.WriteLine($"MovePiece: Black pawn cannot move from row {fromRow} to row {toRow}");
        return;
    }

    // Królowie mogą poruszać się w dowolnym kierunku - brak ograniczeń kierunku

    SetPiece(fromRow, fromCol, PieceType.Empty);
    SetPiece(toRow, toCol, piece);

    // Sprawdź czy nastąpiło bicie
    int rowDiff = Math.Abs(toRow - fromRow);
    int colDiff = Math.Abs(toCol - fromCol);
    
    if (rowDiff > 1 && colDiff > 1 && rowDiff == colDiff)
    {
        // Dla królów - usuń wszystkie figury na ścieżce bicia
        int rowStep = (toRow - fromRow) > 0 ? 1 : -1;
        int colStep = (toCol - fromCol) > 0 ? 1 : -1;
        
        for (int i = 1; i < rowDiff; i++)
        {
            int middleRow = fromRow + (i * rowStep);
            int middleCol = fromCol + (i * colStep);
            
            if (middleRow >= 0 && middleRow < 8 && middleCol >= 0 && middleCol < 8)
            {
                PieceType middlePiece = GetPiece(middleRow, middleCol);
                if (middlePiece != PieceType.Empty && !IsSameColor(piece, middlePiece))
                {
                    SetPiece(middleRow, middleCol, PieceType.Empty);
                    Console.WriteLine($"Captured piece at ({middleRow},{middleCol})");
                }
            }
        }
    }
    else if (rowDiff == 2 && colDiff == 2)
    {
        // Standardowe bicie dla pionków
        int middleRow = (fromRow + toRow) / 2;
        int middleCol = (fromCol + toCol) / 2;
        SetPiece(middleRow, middleCol, PieceType.Empty);
        Console.WriteLine($"Captured piece at ({middleRow},{middleCol})");
    }

    // Promuj do damki (tylko pionki)
    if (piece == PieceType.WhitePawn && toRow == 0)
    {
        SetPiece(toRow, toCol, PieceType.WhiteKing);
        Console.WriteLine($"White pawn promoted to king at ({toRow},{toCol})");
    }
    else if (piece == PieceType.BlackPawn && toRow == 7)
    {
        SetPiece(toRow, toCol, PieceType.BlackKing);
        Console.WriteLine($"Black pawn promoted to king at ({toRow},{toCol})");
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