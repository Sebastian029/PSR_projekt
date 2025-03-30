public class CheckersGame
{
    private CheckersBoard board;
    private bool isWhiteTurn;
    private int? mustCaptureFrom = null;
    private List<int> captureSequence = new List<int>();
    private int _depth;
    private int _granulation;
    private bool? _isPerformanceTest;

    
    public CheckersGame()
    {
        board = new CheckersBoard();
        isWhiteTurn = true;
    }
    public void SetDifficulty(int depth, int granulation, bool? isPerformanceTest)
    {
        _depth = depth;
        _granulation = granulation;
        _isPerformanceTest = isPerformanceTest;
        // Tutaj możesz dodać logikę aktualizacji gry
        Console.WriteLine($"Game difficulty set to Depth: {depth}, Granulation: {granulation}, perfomance: {isPerformanceTest}");
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
        // Get the piece and perform basic capture
        byte piece = board.GetField(from);
    
        // Find captured piece index - we need to update this to work with king captures
        int middleIndex;
        List<(int, int)> validCaptures = board.GetValidCaptures(from);
    
        // Find the matching capture to get the middleIndex
        middleIndex = -1;
        foreach (var capture in validCaptures)
        {
            if (capture.Item1 == to)
            {
                middleIndex = capture.Item2;
                break;
            }
        }
    
        // If no matching capture was found, return false
        if (middleIndex == -1) return false;
    
        byte capturedPiece = board.GetField(middleIndex);

        // Perform the capture
        board.SetField(from, (byte)PieceType.Empty);
        board.SetField(middleIndex, (byte)PieceType.Empty);
        board.SetField(to, piece);

        // Check for promotion to king
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

        // Check for further captures
        var furtherCaptures = board.GetValidCaptures(to);
        if (furtherCaptures.Count > 0)
        {
            mustCaptureFrom = to;
            captureSequence.Add(to);
            return true;
        }

        // End turn if no more captures
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
            
                // Extract just the target positions from the captures
                var targetPositions = new List<int>();
                foreach (var capture in captures)
                {
                    targetPositions.Add(capture.Item1);
                }
            
                // Check if we still need to handle multiple captures separately
                // If GetMultipleCaptures still exists and is needed
                var multipleCaptures = board.GetMultipleCaptures(i);
            
                if (targetPositions.Count > 0 || multipleCaptures.Count > 0)
                {
                    var allCaptures = new List<int>();
                
                    allCaptures.AddRange(targetPositions);
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