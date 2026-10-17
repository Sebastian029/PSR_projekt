namespace Checkers.Core.Model;

public enum Side
{
    White = 0,
    Black = 1,
}

public static class SideExtensions
{
    public static Side Opposite(this Side side) => side == Side.White ? Side.Black : Side.White;

    /// <summary>Row delta of a man's forward move. White starts at the bottom and moves up.</summary>
    public static int ForwardRowDelta(this Side side) => side == Side.White ? -1 : 1;

    /// <summary>The row on which a man of this side is promoted.</summary>
    public static int PromotionRow(this Side side) => side == Side.White ? 0 : Board.Size - 1;
}
