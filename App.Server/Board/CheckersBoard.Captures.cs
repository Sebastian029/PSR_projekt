// CheckersBoard.Captures.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public List<(int toRow, int toCol, int capturedRow, int capturedCol)> GetValidCaptures(int row, int col)
        {
            List<(int, int, int, int)> captures = new List<(int, int, int, int)>();
            
            try
            {
                // Walidacja współrzędnych
                if (row < 0 || row >= 8 || col < 0 || col >= 8)
                {
                    //Console.WriteLine($"GetValidCaptures: Invalid coordinates ({row}, {col})");
                    return captures;
                }
                
                PieceType piece = GetPiece(row, col);
                
                if (piece == PieceType.Empty || !IsDarkSquare(row, col)) 
                {
                    return captures;
                }

                if (piece == PieceType.WhitePawn)
                {
                    AddPawnCaptures(captures, row, col, -1, piece);
                }
                else if (piece == PieceType.BlackPawn)
                {
                    AddPawnCaptures(captures, row, col, 1, piece);
                }
                else if (piece == PieceType.WhiteKing || piece == PieceType.BlackKing)
                {
                    AddKingCaptures(captures, row, col, piece);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error in GetValidCaptures for ({row},{col}): {ex.Message}");
            }

            return captures;
        }

        private void AddPawnCaptures(List<(int, int, int, int)> captures, int row, int col, int direction, PieceType piece)
        {
            try
            {
                int[] colOffsets = { -1, 1 };
                
                foreach (int colOffset in colOffsets)
                {
                    int capturedRow = row + direction;
                    int capturedCol = col + colOffset;
                    int landingRow = row + (2 * direction);
                    int landingCol = col + (2 * colOffset);
                    
                    if (IsValidPosition(capturedRow, capturedCol) && IsValidPosition(landingRow, landingCol) &&
                        IsDarkSquare(capturedRow, capturedCol) && IsDarkSquare(landingRow, landingCol))
                    {
                        PieceType capturedPiece = GetPiece(capturedRow, capturedCol);
                        
                        if (capturedPiece != PieceType.Empty && !IsSameColor(piece, capturedPiece) && 
                            IsEmpty(landingRow, landingCol))
                        {
                            captures.Add((landingRow, landingCol, capturedRow, capturedCol));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"Error in AddPawnCaptures for ({row},{col}): {ex.Message}");
            }
        }

        private void AddKingCaptures(List<(int, int, int, int)> captures, int row, int col, PieceType piece)
        {
            try
            {
                int[] rowDirections = { -1, 1 };
                int[] colDirections = { -1, 1 };
                
                foreach (int rowDir in rowDirections)
                {
                    foreach (int colDir in colDirections)
                    {
                        PieceType foundPiece = PieceType.Empty;
                        int foundRow = -1, foundCol = -1;
                        
                        // Szukaj pierwszej figury w tym kierunku
                        for (int i = 1; i < 8; i++)
                        {
                            int checkRow = row + (i * rowDir);
                            int checkCol = col + (i * colDir);
                            
                            if (checkRow < 0 || checkRow >= 8 || checkCol < 0 || checkCol >= 8)
                                break;
                                
                            if (!IsDarkSquare(checkRow, checkCol))
                                break;
                                
                            PieceType currentPiece = GetPiece(checkRow, checkCol);
                            if (currentPiece != PieceType.Empty)
                            {
                                if (!IsSameColor(piece, currentPiece))
                                {
                                    foundPiece = currentPiece;
                                    foundRow = checkRow;
                                    foundCol = checkCol;
                                }
                                break;
                            }
                        }
                        
                        // Jeśli znaleziono figurę przeciwnika, sprawdź możliwe lądowania
                        if (foundPiece != PieceType.Empty && foundRow != -1 && foundCol != -1)
                        {
                            for (int i = 1; i < 8; i++)
                            {
                                int landingRow = foundRow + (i * rowDir);
                                int landingCol = foundCol + (i * colDir);
                                
                                if (landingRow < 0 || landingRow >= 8 || landingCol < 0 || landingCol >= 8)
                                    break;
                                    
                                if (!IsDarkSquare(landingRow, landingCol))
                                    break;
                                    
                                if (IsEmpty(landingRow, landingCol))
                                {
                                    captures.Add((landingRow, landingCol, foundRow, foundCol));
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"Error in AddKingCaptures for ({row},{col}): {ex.Message}");
            }
        }

        public List<List<(int row, int col)>> GetMultipleCaptures(int row, int col, List<(int, int)> previousCaptures = null)
        {
            List<List<(int, int)>> multipleCaptures = new List<List<(int, int)>>();
            PieceType piece = GetPiece(row, col);
            
            if (piece == PieceType.Empty) return multipleCaptures;

            previousCaptures = previousCaptures ?? new List<(int, int)>();
            var initialCaptures = GetValidCaptures(row, col);

            foreach (var (toRow, toCol, capturedRow, capturedCol) in initialCaptures)
            {
                // Symuluj ruch
                PieceType originalPiece = GetPiece(row, col);
                PieceType capturedPiece = GetPiece(capturedRow, capturedCol);
                
                SetPiece(row, col, PieceType.Empty);
                SetPiece(capturedRow, capturedCol, PieceType.Empty);
                SetPiece(toRow, toCol, originalPiece);

                // Sprawdź promocję
                PieceType currentPiece = originalPiece;
                if (originalPiece == PieceType.WhitePawn && toRow == 0)
                {
                    SetPiece(toRow, toCol, PieceType.WhiteKing);
                    currentPiece = PieceType.WhiteKing;
                }
                else if (originalPiece == PieceType.BlackPawn && toRow == 7)
                {
                    SetPiece(toRow, toCol, PieceType.BlackKing);
                    currentPiece = PieceType.BlackKing;
                }

                // Sprawdź dalsze bicia
                var furtherCaptures = GetValidCaptures(toRow, toCol);
                if (furtherCaptures.Count > 0)
                {
                    foreach (var (furtherToRow, furtherToCol, _, _) in furtherCaptures)
                    {
                        List<(int, int)> sequence = new List<(int, int)>(previousCaptures) { (toRow, toCol), (furtherToRow, furtherToCol) };
                        multipleCaptures.Add(sequence);
                        
                        // Rekurencyjnie sprawdź głębsze bicia
                        var deeperCaptures = GetMultipleCaptures(furtherToRow, furtherToCol, sequence);
                        multipleCaptures.AddRange(deeperCaptures);
                    }
                }
                else
                {
                    multipleCaptures.Add(new List<(int, int)> { (toRow, toCol) });
                }

                // Cofnij symulację
                SetPiece(toRow, toCol, PieceType.Empty);
                SetPiece(capturedRow, capturedCol, capturedPiece);
                SetPiece(row, col, originalPiece);
            }

            return multipleCaptures.Distinct().ToList();
        }

    }
}
