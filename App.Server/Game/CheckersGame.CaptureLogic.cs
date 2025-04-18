namespace App.Server;

public partial class CheckersGame
{
    private bool ExecuteCapture(int from, int to)
    {
        byte piece = board.GetField(from);
        var validCaptures = board.GetValidCaptures(from);

        int middleIndex = -1;
        foreach (var capture in validCaptures)
        {
            if (capture.Item1 == to)
            {
                middleIndex = capture.Item2;
                break;
            }
        }

        if (middleIndex == -1) return false;

        board.SetField(from, (byte)PieceType.Empty);
        board.SetField(middleIndex, (byte)PieceType.Empty);
        board.SetField(to, piece);

        if (piece == (byte)PieceType.WhitePawn && to < 4)
        {
            board.SetField(to, (byte)PieceType.WhiteKing);
        }
        else if (piece == (byte)PieceType.BlackPawn && to >= 28)
        {
            board.SetField(to, (byte)PieceType.BlackKing);
        }

        var furtherCaptures = board.GetValidCaptures(to);
        if (furtherCaptures.Count > 0)
        {
            mustCaptureFrom = to;
            captureSequence.Add(to);
            return true;
        }

        mustCaptureFrom = null;
        captureSequence.Clear();
        isWhiteTurn = !isWhiteTurn;
        return true;
    }

    public Dictionary<int, List<int>> GetAllPossibleCaptures()
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

                var allCaptures = new List<int>();
                allCaptures.AddRange(captures.Select(c => c.Item1));
                allCaptures.AddRange(multipleCaptures);

                if (allCaptures.Count > 0)
                {
                    result[i] = allCaptures;
                }
            }
        }
        return result;
    }
}
