// CheckersGame.CaptureLogic.cs
namespace App.Server;

public partial class CheckersGame
{
    private bool ExecuteCapture(int fromRow, int fromCol, int toRow, int toCol)
    {
        PieceType piece = board.GetPiece(fromRow, fromCol);
        
        var validCaptures = board.GetValidCaptures(fromRow, fromCol);
        
        var targetCapture = validCaptures.FirstOrDefault(c => c.toRow == toRow && c.toCol == toCol);
        if (targetCapture == default) return false;

        // Wykonaj bicie
        board.SetPiece(fromRow, fromCol, PieceType.Empty);
        board.SetPiece(targetCapture.capturedRow, targetCapture.capturedCol, PieceType.Empty);
        board.SetPiece(toRow, toCol, piece);

        // Sprawdź promocję do damki
        if (piece == PieceType.WhitePawn && toRow == 0)
        {
            board.SetPiece(toRow, toCol, PieceType.WhiteKing);
        }
        else if (piece == PieceType.BlackPawn && toRow == 7)
        {
            board.SetPiece(toRow, toCol, PieceType.BlackKing);
        }

        // Sprawdź dalsze bicia
        var furtherCaptures = board.GetValidCaptures(toRow, toCol);
        if (furtherCaptures.Count > 0)
        {
            mustCaptureFrom = (toRow, toCol);
            captureSequence.Add((toRow, toCol));
            return true;
        }

        // Koniec sekwencji bić
        mustCaptureFrom = null;
        captureSequence.Clear();
        isWhiteTurn = !isWhiteTurn;
        return true;
    }

    public Dictionary<(int row, int col), List<(int row, int col)>> GetAllPossibleCaptures()
    {
        var result = new Dictionary<(int, int), List<(int, int)>>();
        
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (!board.IsDarkSquare(row, col)) continue;
                
                PieceType piece = board.GetPiece(row, col);
                
                bool isCurrentPlayerPiece = (isWhiteTurn && (piece == PieceType.WhitePawn || piece == PieceType.WhiteKing)) ||
                                          (!isWhiteTurn && (piece == PieceType.BlackPawn || piece == PieceType.BlackKing));
                
                if (isCurrentPlayerPiece)
                {
                    var captures = board.GetValidCaptures(row, col);
                    var multipleCaptures = board.GetMultipleCaptures(row, col);
                    
                    var allCaptures = new List<(int, int)>();
                    allCaptures.AddRange(captures.Select(c => (c.toRow, c.toCol)));
                    allCaptures.AddRange(multipleCaptures.SelectMany(sequence => sequence));
                    
                    if (allCaptures.Count > 0)
                    {
                        result[(row, col)] = allCaptures.Distinct().ToList();
                    }
                }
            }
        }
        
        return result;
    }
}
