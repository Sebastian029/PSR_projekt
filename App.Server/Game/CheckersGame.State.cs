// CheckersGame.State.cs
namespace App.Server
{
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
            
            // POPRAWKA: Resetuj stan gry
            gameOver = false;
            winner = null;
            drawReason = null;
            ResetMoveHistory();
            
            Console.WriteLine("Board reset completed - new game ready");
            return board.SerializeBoard();
        }

        // Dodaj metodę pełnego resetu
        public void ResetGame()
        {
            board.ResetBoard();
            isWhiteTurn = true;
            mustCaptureFrom = null;
            captureSequence.Clear();
            gameOver = false;
            winner = null;
            drawReason = null;
            ResetMoveHistory();
            Console.WriteLine("Game fully reset - ready for new game");
        }

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
            
            // NOWE: Resetuj historię po zmianie stanu
            ResetMoveHistory();
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

        // NOWE: Metody do sprawdzania stanu gry
        public bool IsDrawByRepetition()
        {
            return positionCount.Values.Any(count => count >= MAX_POSITION_REPEATS);
        }

        public bool IsDrawBy50MoveRule()
        {
            return movesWithoutCapture >= MAX_MOVES_WITHOUT_CAPTURE;
        }

        
    }
}
