using System.Text;

namespace Checkers.Core.Model;

/// <summary>
/// An 8x8 draughts board. Instances are cheap to clone; the search treats them as values
/// (every applied move produces a new board) and never shares a mutable board between threads.
/// </summary>
public sealed class Board : IEquatable<Board>
{
    public const int Size = 8;

    /// <summary>Number of playable (dark) squares.</summary>
    public const int DarkSquareCount = 32;

    private readonly Piece[] _cells;

    private Board(Piece[] cells) => _cells = cells;

    public static Board Empty() => new(new Piece[Size * Size]);

    /// <summary>Black on rows 0-2, white on rows 5-7, white to move first.</summary>
    public static Board CreateInitial()
    {
        var board = Empty();
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                var square = new Square(row, col);
                if (!square.IsDark)
                {
                    continue;
                }

                if (row < 3)
                {
                    board.Set(square, Piece.BlackMan);
                }
                else if (row >= Size - 3)
                {
                    board.Set(square, Piece.WhiteMan);
                }
            }
        }

        return board;
    }

    /// <summary>Returns the piece on the square, or <see cref="Piece.Empty"/> for squares off the board.</summary>
    public Piece Get(Square square) => square.IsOnBoard ? _cells[square.Row * Size + square.Col] : Piece.Empty;

    public Piece Get(int row, int col) => Get(new Square(row, col));

    public void Set(Square square, Piece piece)
    {
        if (!square.IsOnBoard)
        {
            throw new ArgumentOutOfRangeException(nameof(square), square, "Square is outside the board.");
        }

        _cells[square.Row * Size + square.Col] = piece;
    }

    public Board Clone() => new((Piece[])_cells.Clone());

    public int Count(Side side)
    {
        var count = 0;
        foreach (var cell in _cells)
        {
            if (cell != Piece.Empty && cell.BelongsTo(side))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>All squares occupied by pieces of the given side.</summary>
    public IEnumerable<Square> PiecesOf(Side side)
    {
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                if (_cells[row * Size + col].BelongsTo(side))
                {
                    yield return new Square(row, col);
                }
            }
        }
    }

    /// <summary>
    /// Compact wire format: 32 bytes, one per dark square in row-major order, holding the <see cref="Piece"/> code.
    /// </summary>
    public byte[] ToWire()
    {
        var wire = new byte[DarkSquareCount];
        var index = 0;
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                if (((row + col) & 1) == 1)
                {
                    wire[index++] = (byte)_cells[row * Size + col];
                }
            }
        }

        return wire;
    }

    public static Board FromWire(ReadOnlySpan<byte> wire)
    {
        if (wire.Length != DarkSquareCount)
        {
            throw new ArgumentException(
                $"Board wire format must be exactly {DarkSquareCount} bytes, got {wire.Length}.", nameof(wire));
        }

        var board = Empty();
        var index = 0;
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                if (((row + col) & 1) != 1)
                {
                    continue;
                }

                var value = wire[index++];
                if (value > (byte)Piece.BlackKing)
                {
                    throw new ArgumentException($"Invalid piece code {value}.", nameof(wire));
                }

                board._cells[row * Size + col] = (Piece)value;
            }
        }

        return board;
    }

    /// <summary>
    /// Parses 8 rows of 8 characters: '.' empty, 'w'/'W' white man/king, 'b'/'B' black man/king.
    /// Intended for tests and diagnostics. Pieces must stand on dark squares.
    /// </summary>
    public static Board Parse(params string[] rows)
    {
        if (rows.Length != Size)
        {
            throw new ArgumentException($"Expected {Size} rows.", nameof(rows));
        }

        var board = Empty();
        for (var row = 0; row < Size; row++)
        {
            if (rows[row].Length != Size)
            {
                throw new ArgumentException($"Row {row} must have {Size} characters.", nameof(rows));
            }

            for (var col = 0; col < Size; col++)
            {
                var piece = rows[row][col] switch
                {
                    '.' => Piece.Empty,
                    'w' => Piece.WhiteMan,
                    'W' => Piece.WhiteKing,
                    'b' => Piece.BlackMan,
                    'B' => Piece.BlackKing,
                    var other => throw new ArgumentException($"Unknown piece character '{other}'.", nameof(rows)),
                };

                var square = new Square(row, col);
                if (piece != Piece.Empty && !square.IsDark)
                {
                    throw new ArgumentException($"Piece placed on light square {square}.", nameof(rows));
                }

                board._cells[row * Size + col] = piece;
            }
        }

        return board;
    }

    public bool Equals(Board? other) => other is not null && _cells.AsSpan().SequenceEqual(other._cells);

    public override bool Equals(object? obj) => Equals(obj as Board);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var cell in _cells)
        {
            hash.Add(cell);
        }

        return hash.ToHashCode();
    }

    /// <summary>A stable string key of the position, used for repetition detection.</summary>
    public string ToKey()
    {
        var wire = ToWire();
        var chars = new char[wire.Length];
        for (var i = 0; i < wire.Length; i++)
        {
            chars[i] = (char)('0' + wire[i]);
        }

        return new string(chars);
    }

    public override string ToString()
    {
        var text = new StringBuilder();
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                text.Append(_cells[row * Size + col] switch
                {
                    Piece.WhiteMan => 'w',
                    Piece.WhiteKing => 'W',
                    Piece.BlackMan => 'b',
                    Piece.BlackKing => 'B',
                    _ => '.',
                });
            }

            if (row < Size - 1)
            {
                text.Append('\n');
            }
        }

        return text.ToString();
    }
}
