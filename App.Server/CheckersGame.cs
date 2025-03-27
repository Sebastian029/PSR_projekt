using System;
using System.Text;
using App.Server;

public class CheckersGame
{
    private CheckersBoard board;
    private bool isWhiteTurn;
    private CheckersAI ai;

    public CheckersGame()
    {
        board = new CheckersBoard();
        isWhiteTurn = true;
        ai = new CheckersAI();
    }

    public bool PlayMove(int from, int to)
    {
        byte piece = board.GetField(from);
        if (isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing)) return false;
        if (!isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing)) return false;

        var moves = board.GetValidMoves(from);
        var captures = board.GetValidCaptures(from);
        
        if (moves.Contains(to) || captures.Contains(to))
        {
            board.MovePiece(from, to);
            isWhiteTurn = !isWhiteTurn;
            return true;
        }
        return false;
    }

    public (int fromField, int toField) GetAIMove()
    {
        return ai.GetBestMove(board, isWhiteTurn);
    }

    public bool PlayAIMove()
    {
        var (fromField, toField) = GetAIMove();
        return PlayMove(fromField, toField);
    }

    public string GetBoardState()
    {
        return board.SerializeBoard();
    }

    public bool IsGameOver()
    {
        // Check if either side has no valid moves
        return !HasValidMoves(true) || !HasValidMoves(false);
    }

    private bool HasValidMoves(bool isWhiteTurn)
    {
        for (int i = 0; i < 64; i++)
        {
            byte piece = board.GetField(i);
            
            // Skip if piece doesn't match current turn
            if (isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing))
            {
                var moves = board.GetValidMoves(i);
                var captures = board.GetValidCaptures(i);
                if (moves.Count > 0 || captures.Count > 0)
                    return true;
            }
            else if (!isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing))
            {
                var moves = board.GetValidMoves(i);
                var captures = board.GetValidCaptures(i);
                if (moves.Count > 0 || captures.Count > 0)
                    return true;
            }
        }
        return false;
    }

    public int GetMiddleIndex(int fromIndex, int toIndex)
    {
        return board.GetMiddleIndex(fromIndex, toIndex);
    }
}