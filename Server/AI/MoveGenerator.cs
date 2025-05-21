using App.Grpc;
using System.Collections.Generic;
using System.Linq;
using App.Server;

public class MoveGenerator : IMoveGenerator
{
    public List<(int, int)> GetAllValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        var moves = new List<(int, int)>();
        for (int i = 0; i < 32; i++)
        {
            if (!PieceUtils.IsColor(board.GetField(i), isWhiteTurn)) continue;
            moves.AddRange(board.GetValidMoves(i).Select(to => (i, to)));
        }
        return moves;
    }
    
    public Dictionary<int, List<int>> GetMandatoryCaptures(CheckersBoard board, bool isWhiteTurn)
    {
        var result = new Dictionary<int, List<int>>();
        for (int i = 0; i < 32; i++)
        {
            var piece = board.GetField(i);
            if (!PieceUtils.IsColor(piece, isWhiteTurn)) continue;
            
            var captures = board.GetValidCaptures(i);
            var targetPositions = captures.Select(c => c.Item1).ToList();
            var multiCaptures = board.GetMultipleCaptures(i);
            
            if (targetPositions.Count > 0 || multiCaptures.Count > 0)
                result[i] = targetPositions.Concat(multiCaptures).ToList();
        }
        return result;
    }
    
    public bool HasValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        if (GetMandatoryCaptures(board, isWhiteTurn).Count > 0) return true;
        
        for (int i = 0; i < 32; i++)
        {
            if (PieceUtils.IsColor(board.GetField(i), isWhiteTurn) && board.GetValidMoves(i).Count > 0)
                return true;
        }
        return false;
    }
    
    public bool IsGameOver(CheckersBoard board) =>
        !HasValidMoves(board, true) || !HasValidMoves(board, false);
        
    public List<(int, int)> GetCaptureMoves(Dictionary<int, List<int>> captures) =>
        captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key, to))).ToList();
}

public interface IMoveGenerator
{
    List<(int, int)> GetAllValidMoves(CheckersBoard board, bool isWhiteTurn);
    Dictionary<int, List<int>> GetMandatoryCaptures(CheckersBoard board, bool isWhiteTurn);
    bool HasValidMoves(CheckersBoard board, bool isWhiteTurn);
    bool IsGameOver(CheckersBoard board);
    List<(int, int)> GetCaptureMoves(Dictionary<int, List<int>> captures);
}
