namespace Checkers.Core.Model;

/// <summary>A square addressed by row (0 = top, black's back rank) and column (0 = left).</summary>
public readonly record struct Square(int Row, int Col)
{
    public bool IsOnBoard => Row is >= 0 and < Board.Size && Col is >= 0 and < Board.Size;

    /// <summary>Only dark squares are used in play; a square is dark when row + col is odd.</summary>
    public bool IsDark => ((Row + Col) & 1) == 1;

    public Square Offset(int dRow, int dCol) => new(Row + dRow, Col + dCol);

    public override string ToString() => $"({Row},{Col})";
}
