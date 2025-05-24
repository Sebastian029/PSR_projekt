// CheckersGame.State.cs
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

    // Dodatkowe metody dla lepszego zarządzania stanem
    public PieceType[,] GetCurrentBoard()
    {
        return (PieceType[,])board.board.Clone();
    }

    public void SetBoardState(PieceType[,] newBoard)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                board.SetPiece(row, col, newBoard[row, col]);
            }
        }
    }

    public List<(int row, int col)> GetAllPieces(bool white)
    {
        var pieces = new List<(int, int)>();
        
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board.HasPiece(row, col, white))
                {
                    pieces.Add((row, col));
                }
            }
        }
        
        return pieces;
    }
}