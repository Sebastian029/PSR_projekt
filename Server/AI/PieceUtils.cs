using App.Grpc;

public static class PieceUtils
{
    public static bool IsWhite(byte piece) =>
        piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing;
        
    public static bool IsKing(byte piece) =>
        piece == (byte)PieceType.WhiteKing || piece == (byte)PieceType.BlackKing;
        
    public static bool IsColor(byte piece, bool isWhite) =>
        isWhite ? IsWhite(piece) : (!IsWhite(piece) && piece != (byte)PieceType.Empty);
        
    public static (int row, int col) GetBoardPosition(int index)
    {
        int row = index / 4;
        int col = 2 * (index % 4) + (row % 2);
        return (row, col);
    }
}