namespace App.Server;

public partial class CheckersGame
{
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
}