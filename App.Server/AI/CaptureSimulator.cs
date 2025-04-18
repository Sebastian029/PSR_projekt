using App.Server;



public class CaptureSimulator
{
    public void SimulateCapture(CheckersBoard board, int from, int to)
    {
        byte piece = board.GetField(from);
        var capture = board.GetValidCaptures(from).FirstOrDefault(c => c.Item1 == to);
        if (capture.Item1 != to) return;

        board.SetField(from, (byte)PieceType.Empty);
        board.SetField(capture.Item2, (byte)PieceType.Empty);
        board.SetField(to, piece);

        int promotionRow = PieceUtils.IsWhite(piece) ? 0 : 7;
        if (!PieceUtils.IsKing(piece) && to / 4 == promotionRow)
        {
            board.SetField(to, (byte)(piece + 1));
        }
    }
}