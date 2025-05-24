using System.Collections.Generic;
using System.Linq;

namespace MinimaxServer
{
    public class MoveGenerator
    {
        public List<(int fromRow, int fromCol, int toRow, int toCol)> GetAllValidMoves(CheckersBoard board, bool isWhiteTurn)
        {
            var moves = new List<(int, int, int, int)>();
            
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!board.IsDarkSquare(row, col)) continue;
                    
                    PieceType piece = board.GetPiece(row, col);
                    if (!PieceUtils.IsColor(piece, isWhiteTurn)) continue;

                    var validMoves = board.GetValidMoves(row, col);
                    moves.AddRange(validMoves.Select(move => (row, col, move.row, move.col)));
                }
            }

            return moves;
        }

        public bool HasValidMoves(CheckersBoard board, bool isWhiteTurn)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!board.IsDarkSquare(row, col)) continue;
                    
                    PieceType piece = board.GetPiece(row, col);
                    if (PieceUtils.IsColor(piece, isWhiteTurn) && board.GetValidMoves(row, col).Count > 0)
                        return true;
                }
            }
            return false;
        }

        public bool IsGameOver(CheckersBoard board) =>
            !HasValidMoves(board, true) || !HasValidMoves(board, false);
    }
}