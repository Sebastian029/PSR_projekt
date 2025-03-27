public class CheckersGame
{
    private CheckersBoard board;
    private bool isWhiteTurn;
    private int? mustCaptureFrom = null;
    private List<int> captureSequence = new List<int>();

    public CheckersGame()
    {
        board = new CheckersBoard();
        isWhiteTurn = true;
    }

    public bool PlayMove(int from, int to)
    {
        // Sprawdź czy gracz może wykonać ruch
        byte piece = board.GetField(from);
        if (piece == (byte)PieceType.Empty) return false;
        if (isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing)) return false;
        if (!isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing)) return false;

        // Najpierw sprawdź czy są dostępne bicia
        var allCaptures = GetAllPossibleCaptures();
        if (allCaptures.Count > 0)
        {
            if (!allCaptures.ContainsKey(from)) return false;
            if (!allCaptures[from].Contains(to)) return false;

            // Wykonaj bicie
            return ExecuteCapture(from, to);
        }

        // Jeśli nie ma bić, sprawdź normalne ruchy
        var moves = board.GetValidMoves(from);
        if (!moves.Contains(to)) return false;

        // Wykonaj ruch
        board.MovePiece(from, to);
        isWhiteTurn = !isWhiteTurn;
        return true;
    }

    private bool ExecuteCapture(int from, int to)
    {
        // Wykonaj bicie na planszy
        byte piece = board.GetField(from);
        int middleIndex = board.GetMiddleIndex(from, to);
        byte capturedPiece = board.GetField(middleIndex);

        // Usuń pionek z pola źródłowego i zbity pionek
        board.SetField(from, (byte)PieceType.Empty);
        board.SetField(middleIndex, (byte)PieceType.Empty);
        board.SetField(to, piece);

        // Sprawdź promocję na damkę
        if (piece == (byte)PieceType.WhitePawn && to < 4)
        {
            board.SetField(to, (byte)PieceType.WhiteKing);
            piece = (byte)PieceType.WhiteKing;
        }
        else if (piece == (byte)PieceType.BlackPawn && to >= 28)
        {
            board.SetField(to, (byte)PieceType.BlackKing);
            piece = (byte)PieceType.BlackKing;
        }

        // Sprawdź kolejne bicia
        var furtherCaptures = board.GetValidCaptures(to);
        if (furtherCaptures.Count > 0)
        {
            mustCaptureFrom = to;
            captureSequence.Add(to);
            return true;
        }

        // Zakończ turę, jeśli nie ma więcej bić
        mustCaptureFrom = null;
        captureSequence.Clear();
        isWhiteTurn = !isWhiteTurn;
        return true;
    }

    public string GetBoardState()
    {
        return board.SerializeBoard();
    }

    public string GetBoardStateReset()
    {
        board.ResetBoard();
        isWhiteTurn = true;
        mustCaptureFrom = null;
        captureSequence.Clear();
        return board.SerializeBoard();
    }

    public bool IsWhiteTurn => isWhiteTurn;
    public int? MustCaptureFrom => mustCaptureFrom;

    public List<int> GetPossibleMoves(int fieldIndex)
    {
        // Sprawdź czy pionek należy do aktualnego gracza
        byte piece = board.GetField(fieldIndex);
        if (isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing))
            return new List<int>();
        if (!isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing))
            return new List<int>();

        var allCaptures = GetAllPossibleCaptures();
        bool hasAnyCaptures = allCaptures.Count > 0;

        if (hasAnyCaptures)
        {
            if (allCaptures.ContainsKey(fieldIndex))
                return allCaptures[fieldIndex];
            return new List<int>();
        }

        // Jeśli nie ma bić, zwróć normalne ruchy
        return board.GetValidMoves(fieldIndex);
    }

    private Dictionary<int, List<int>> GetAllPossibleCaptures()
    {
        var result = new Dictionary<int, List<int>>();
        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            if ((isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing)) ||
                (!isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing)))
            {
                var captures = board.GetValidCaptures(i);
                var multipleCaptures = board.GetMultipleCaptures(i);
                if (captures.Count > 0 || multipleCaptures.Count > 0)
                {
                    var allCaptures = new List<int>();
                    allCaptures.AddRange(captures);
                    allCaptures.AddRange(multipleCaptures);
                    result[i] = allCaptures;
                }
            }
        }
        return result;
    }
}

public enum PieceType : byte
{
    Empty = 0,
    WhitePawn = 1,
    WhiteKing = 2,
    BlackPawn = 3,
    BlackKing = 4
}