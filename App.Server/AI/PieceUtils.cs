// PieceUtils.cs
using App.Server;

public static class PieceUtils
{
    public static bool IsWhite(PieceType piece) =>
        piece == PieceType.WhitePawn || piece == PieceType.WhiteKing;

    public static bool IsKing(PieceType piece) =>
        piece == PieceType.WhiteKing || piece == PieceType.BlackKing;

    public static bool IsColor(PieceType piece, bool isWhite) =>
        isWhite ? IsWhite(piece) : (!IsWhite(piece) && piece != PieceType.Empty);

    public static (int row, int col) GetBoardPosition(int index)
    {
        int row = index / 4;
        int col = 2 * (index % 4) + (row % 2);
        return (row, col);
    }

    // Przeciążenia dla kompatybilności z byte
    public static bool IsWhite(byte piece) =>
        IsWhite((PieceType)piece);

    public static bool IsKing(byte piece) =>
        IsKing((PieceType)piece);

    public static bool IsColor(byte piece, bool isWhite) =>
        IsColor((PieceType)piece, isWhite);
}