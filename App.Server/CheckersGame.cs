using System;
using System.Text;
using App.Server;

public class CheckersGame
{
    private CheckersBoard board;
    private bool isWhiteTurn;

    public CheckersGame()
    {
        board = new CheckersBoard();
        isWhiteTurn = true;
    }

    public bool PlayMove(int from, int to)
    {
        byte piece = board.GetField(from);
        if (isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing)) return false;
        if (!isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing)) return false;

        var moves  = board.GetValidMoves(from);
        var captures = board.GetValidCaptures(from);

        if (moves.Contains(to) || captures.Contains(to))
        {
            board.MovePiece(from, to);
            isWhiteTurn = !isWhiteTurn;
            return true;
        }
        return false;
    }

    public string GetBoardState()
    {
        return board.SerializeBoard();
    }
}