namespace Checkers.Core.Model;

public readonly record struct Square(int Row, int Col)
{
    public bool IsOnBoard => Row is >= 0 and < Board.Size && Col is >= 0 and < Board.Size;

    public bool IsDark => ((Row + Col) & 1) == 1;

    public Square Offset(int dRow, int dCol) => new(Row + dRow, Col + dCol);

    public override string ToString() => $"({Row},{Col})";
}
