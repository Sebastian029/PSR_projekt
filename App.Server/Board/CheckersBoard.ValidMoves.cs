// CheckersBoard.ValidMoves.cs
using System;
using System.Collections.Generic;

namespace App.Server
{
    public partial class CheckersBoard
    {
        // CheckersBoard.ValidMoves.cs
        // CheckersBoard.ValidMoves.cs
public List<(int row, int col)> GetValidMoves(int row, int col)
{
    List<(int, int)> moves = new List<(int, int)>();
    
    try
    {
        // Walidacja współrzędnych
        if (row < 0 || row >= 8 || col < 0 || col >= 8)
        {
           // Console.WriteLine($"GetValidMoves: Invalid coordinates ({row}, {col})");
            return moves;
        }
        
        PieceType piece = GetPiece(row, col);
        
        if (piece == PieceType.Empty || !IsDarkSquare(row, col)) 
        {
            return moves;
        }

        if (piece == PieceType.WhitePawn)
        {
            AddPawnMoves(moves, row, col, -1);
        }
        else if (piece == PieceType.BlackPawn)
        {
            AddPawnMoves(moves, row, col, 1);
        }
        else if (piece == PieceType.WhiteKing || piece == PieceType.BlackKing)
        {
            AddKingMoves(moves, row, col);
        }
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"Error in GetValidMoves for ({row},{col}): {ex.Message}");
    }

    return moves;
}

private void AddPawnMoves(List<(int, int)> moves, int row, int col, int direction)
{
    try
    {
        // Sprawdź ruchy po przekątnej
        int[] colOffsets = { -1, 1 };
        
        foreach (int colOffset in colOffsets)
        {
            int newRow = row + direction;
            int newCol = col + colOffset;
            
            if (IsValidPosition(newRow, newCol) && IsDarkSquare(newRow, newCol) && IsEmpty(newRow, newCol))
            {
                moves.Add((newRow, newCol));
            }
        }
    }
    catch (Exception ex)
    {
       // Console.WriteLine($"Error in AddPawnMoves for ({row},{col}): {ex.Message}");
    }
}

// CheckersBoard.ValidMoves.cs
private void AddKingMoves(List<(int, int)> moves, int row, int col)
{
    try
    {
        // Damki mogą poruszać się we wszystkich 4 kierunkach przekątnych
        int[] rowDirections = { -1, 1 };
        int[] colDirections = { -1, 1 };
        
        foreach (int rowDir in rowDirections)
        {
            foreach (int colDir in colDirections)
            {
                // Sprawdź wszystkie pola w danym kierunku
                for (int i = 1; i < 8; i++)
                {
                    int newRow = row + (i * rowDir);
                    int newCol = col + (i * colDir);
                    
                    // Sprawdź czy współrzędne są w granicach
                    if (newRow < 0 || newRow >= 8 || newCol < 0 || newCol >= 8)
                        break;
                        
                    if (!IsDarkSquare(newRow, newCol))
                        break;
                        
                    if (IsEmpty(newRow, newCol))
                    {
                        moves.Add((newRow, newCol));
                    }
                    else
                    {
                        break; // Zatrzymaj się przy napotkaniu figury
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
       // Console.WriteLine($"Error in AddKingMoves for ({row},{col}): {ex.Message}");
    }
}



       
            }
        }

