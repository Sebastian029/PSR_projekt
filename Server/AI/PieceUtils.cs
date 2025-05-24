namespace MinimaxServer
{
    public static class PieceUtils
    {
        public static bool IsWhite(PieceType piece) =>
            piece == PieceType.WhitePawn || piece == PieceType.WhiteKing;

        public static bool IsKing(PieceType piece) =>
            piece == PieceType.WhiteKing || piece == PieceType.BlackKing;

        public static bool IsColor(PieceType piece, bool isWhite) =>
            isWhite ? IsWhite(piece) : (!IsWhite(piece) && piece != PieceType.Empty);
    }
}