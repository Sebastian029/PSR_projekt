namespace Checkers.Core.Model;

/// <summary>
/// A complete move for one turn: a single step, or a whole capture sequence.
/// <see cref="Path"/> lists every landing square in order and <see cref="Captured"/> every removed piece.
/// </summary>
public sealed class Move : IEquatable<Move>
{
    public Move(Square from, IReadOnlyList<Square> path, IReadOnlyList<Square> captured)
    {
        if (path.Count == 0)
        {
            throw new ArgumentException("A move needs at least one landing square.", nameof(path));
        }

        From = from;
        Path = path;
        Captured = captured;
    }

    public Square From { get; }

    public IReadOnlyList<Square> Path { get; }

    public IReadOnlyList<Square> Captured { get; }

    public Square To => Path[^1];

    public bool IsCapture => Captured.Count > 0;

    public bool Equals(Move? other) =>
        other is not null && From == other.From && Path.SequenceEqual(other.Path);

    public override bool Equals(object? obj) => Equals(obj as Move);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(From);
        foreach (var square in Path)
        {
            hash.Add(square);
        }

        return hash.ToHashCode();
    }

    public override string ToString() =>
        From + (IsCapture ? "x" : "-") + string.Join(IsCapture ? "x" : "-", Path);
}
