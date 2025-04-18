namespace App.Server;

public partial class CheckersGame
{
    public bool PlayMove(int from, int to)
    {
        byte piece = board.GetField(from);
        if (piece == (byte)PieceType.Empty) return false;
        if (isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing)) return false;
        if (!isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing)) return false;

        var allCaptures = GetAllPossibleCaptures();
        if (allCaptures.Count > 0)
        {
            if (!allCaptures.ContainsKey(from) || !allCaptures[from].Contains(to)) return false;
            return ExecuteCapture(from, to);
        }

        var moves = board.GetValidMoves(from);
        if (!moves.Contains(to)) return false;

        board.MovePiece(from, to);
        isWhiteTurn = !isWhiteTurn;
        return true;
    }

    public bool PlayAIMove()
    {
        var (fromField, toField) = GetAIMove();
        return PlayMove(fromField, toField);
    }
}
