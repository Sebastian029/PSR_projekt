namespace Checkers.Core.Model;

public enum Piece : byte
{
    Empty = 0,
    WhiteMan = 1,
    WhiteKing = 2,
    BlackMan = 3,
    BlackKing = 4,
}

public static class PieceExtensions
{
    public static bool IsEmpty(this Piece piece) => piece == Piece.Empty;

    public static bool IsKing(this Piece piece) => piece is Piece.WhiteKing or Piece.BlackKing;

    public static bool IsMan(this Piece piece) => piece is Piece.WhiteMan or Piece.BlackMan;

    public static bool BelongsTo(this Piece piece, Side side) => side == Side.White
        ? piece is Piece.WhiteMan or Piece.WhiteKing
        : piece is Piece.BlackMan or Piece.BlackKing;

    public static Side SideOf(this Piece piece) => piece switch
    {
        Piece.WhiteMan or Piece.WhiteKing => Side.White,
        Piece.BlackMan or Piece.BlackKing => Side.Black,
        _ => throw new ArgumentException("An empty square has no side.", nameof(piece)),
    };

    public static Piece Promoted(this Piece piece) => piece switch
    {
        Piece.WhiteMan => Piece.WhiteKing,
        Piece.BlackMan => Piece.BlackKing,
        _ => piece,
    };
}
