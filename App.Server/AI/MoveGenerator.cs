// MoveGenerator.cs
using App.Server;
using System.Collections.Generic;
using System.Linq;

public class MoveGenerator : IMoveGenerator
{
    public List<(int fromRow, int fromCol, int toRow, int toCol)> GetAllValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        var moves = new List<(int, int, int, int)>();
        
        try
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!IsDarkSquare(row, col)) continue;
                    
                    PieceType piece = board.GetPiece(row, col);
                    if (!PieceUtils.IsColor(piece, isWhiteTurn)) continue;

                    try
                    {
                        var validMoves = board.GetValidMoves(row, col);
                        moves.AddRange(validMoves.Select(move => (row, col, move.row, move.col)));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting valid moves for piece at ({row},{col}): {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAllValidMoves: {ex.Message}");
        }

        return moves;
    }

    public Dictionary<(int row, int col), List<(int row, int col)>> GetMandatoryCaptures(CheckersBoard board, bool isWhiteTurn)
    {
        var result = new Dictionary<(int, int), List<(int, int)>>();
        
        try
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!IsDarkSquare(row, col)) continue;
                    
                    PieceType piece = board.GetPiece(row, col);
                    if (!PieceUtils.IsColor(piece, isWhiteTurn)) continue;

                    try
                    {
                        var captures = board.GetValidCaptures(row, col);
                        var multiCaptures = board.GetMultipleCaptures(row, col);
                        
                        var allCaptures = new List<(int, int)>();
                        allCaptures.AddRange(captures.Select(c => (c.toRow, c.toCol)));
                        allCaptures.AddRange(multiCaptures.SelectMany(sequence => sequence));

                        if (allCaptures.Count > 0)
                        {
                            result[(row, col)] = allCaptures.Distinct().ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting captures for piece at ({row},{col}): {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetMandatoryCaptures: {ex.Message}");
        }

        return result;
    }

    public bool HasValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        try
        {
            if (GetMandatoryCaptures(board, isWhiteTurn).Count > 0) return true;
            
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!IsDarkSquare(row, col)) continue;
                    
                    PieceType piece = board.GetPiece(row, col);
                    if (PieceUtils.IsColor(piece, isWhiteTurn))
                    {
                        try
                        {
                            if (board.GetValidMoves(row, col).Count > 0)
                                return true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error checking moves for piece at ({row},{col}): {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HasValidMoves: {ex.Message}");
        }

        return false;
    }

    public bool IsGameOver(CheckersBoard board) =>
        !HasValidMoves(board, true) || !HasValidMoves(board, false);

    public List<(int fromRow, int fromCol, int toRow, int toCol)> GetCaptureMoves(Dictionary<(int row, int col), List<(int row, int col)>> captures) =>
        captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key.row, kvp.Key.col, to.row, to.col))).ToList();

    private bool IsDarkSquare(int row, int col)
    {
        return (row + col) % 2 == 1;
    }
}


public interface IMoveGenerator
{
    List<(int fromRow, int fromCol, int toRow, int toCol)> GetAllValidMoves(CheckersBoard board, bool isWhiteTurn);
    Dictionary<(int row, int col), List<(int row, int col)>> GetMandatoryCaptures(CheckersBoard board, bool isWhiteTurn);
    bool HasValidMoves(CheckersBoard board, bool isWhiteTurn);
    bool IsGameOver(CheckersBoard board);
}
