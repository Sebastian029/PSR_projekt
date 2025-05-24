// CheckersBoard.MovementLogic.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Server
{
    public partial class CheckersBoard
    {
        // CheckersBoard.cs
// CheckersBoard.cs - poprawiona metoda MovePiece
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

    // ZABEZPIECZENIE: Sprawdź czy na pozycji docelowej nie ma już damki
    PieceType targetPiece = GetPiece(toRow, toCol);
    if (targetPiece == PieceType.WhiteKing || targetPiece == PieceType.BlackKing)
    {
        Console.WriteLine($"MovePiece: Target position ({toRow},{toCol}) already has a king, skipping move");
        return;
    }

    //Console.WriteLine($"MovePiece: Moving {piece} from ({fromRow},{fromCol}) to ({toRow},{toCol})");

    SetPiece(fromRow, fromCol, PieceType.Empty);
    SetPiece(toRow, toCol, piece);

    // Sprawdź czy nastąpiło bicie
    int rowDiff = Math.Abs(toRow - fromRow);
    int colDiff = Math.Abs(toCol - fromCol);
    
    if (rowDiff > 1 && colDiff > 1 && rowDiff == colDiff)
    {
        // Usuń zbite figury na ścieżce
        int rowStep = (toRow - fromRow) > 0 ? 1 : -1;
        int colStep = (toCol - fromCol) > 0 ? 1 : -1;
        
        for (int i = 1; i < rowDiff; i++)
        {
            int middleRow = fromRow + (i * rowStep);
            int middleCol = fromCol + (i * colStep);
            
            if (middleRow >= 0 && middleRow < 8 && middleCol >= 0 && middleCol < 8)
            {
                PieceType middlePiece = GetPiece(middleRow, middleCol);
                if (middlePiece != PieceType.Empty)
                {
                    SetPiece(middleRow, middleCol, PieceType.Empty);
                    Console.WriteLine($"Captured piece at ({middleRow},{middleCol})");
                }
            }
        }
    }

    // ZABEZPIECZENIE: Promuj do damki TYLKO jeśli to nadal pionek
    PieceType finalPiece = GetPiece(toRow, toCol);
    if (finalPiece == PieceType.WhitePawn && toRow == 0)
    {
        SetPiece(toRow, toCol, PieceType.WhiteKing);
        //Console.WriteLine($"White pawn promoted to king at ({toRow},{toCol})");
    }
    else if (finalPiece == PieceType.BlackPawn && toRow == 7)
    {
        SetPiece(toRow, toCol, PieceType.BlackKing);
        //Console.WriteLine($"Black pawn promoted to king at ({toRow},{toCol})");
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