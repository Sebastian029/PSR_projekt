namespace Checkers.Core.Model;

public enum Side
{
    White = 0,
    Black = 1,
}

public static class SideExtensions
{
    public static Side Opposite(this Side side) => side == Side.White ? Side.Black : Side.White;

    public static int ForwardRowDelta(this Side side) => side == Side.White ? -1 : 1;

    public static int PromotionRow(this Side side) => side == Side.White ? 0 : Board.Size - 1;
}
