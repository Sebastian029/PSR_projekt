using App.Server;
using System.Linq;

public class CaptureSimulator
{
    public void SimulateCapture(CheckersBoard board, int fromRow, int fromCol, int toRow, int toCol)
    {
        PieceType piece = board.GetPiece(fromRow, fromCol);

        var captures = board.GetValidCaptures(fromRow, fromCol);
        var capture = captures.FirstOrDefault(c => c.toRow == toRow && c.toCol == toCol);
        if (capture == default) return;

        board.SetPiece(fromRow, fromCol, PieceType.Empty);
        board.SetPiece(capture.capturedRow, capture.capturedCol, PieceType.Empty);
        board.SetPiece(toRow, toCol, piece);

        if (piece == PieceType.WhitePawn && toRow == 0)
        {
            board.SetPiece(toRow, toCol, PieceType.WhiteKing);
        }
        else if (piece == PieceType.BlackPawn && toRow == 7)
        {
            board.SetPiece(toRow, toCol, PieceType.BlackKing);
        }
    }

    public void SimulateCapture(CheckersBoard board, int from, int to)
    {
        var (fromRow, fromCol) = ConvertFromIndex32(from);
        var (toRow, toCol) = ConvertFromIndex32(to);
        SimulateCapture(board, fromRow, fromCol, toRow, toCol);
    }

    public void SimulateCapture(CheckersBoard board, object from, object to)
    {
        if (from is int fromIndex && to is int toIndex)
        {
            SimulateCapture(board, fromIndex, toIndex);
        }
        else if (from is (int fromRow, int fromCol) && to is (int toRow, int toCol))
        {
            SimulateCapture(board, fromRow, fromCol, toRow, toCol);
        }
    }

    private (int row, int col) ConvertFromIndex32(int index32)
    {
        int row = index32 / 4;
        int col = (index32 % 4) * 2 + (row % 2 == 0 ? 1 : 0);
        return (row, col);
    }
}