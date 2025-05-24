using System;

namespace MinimaxServer
{
    public interface IBoardEvaluator
    {
        int EvaluateBoard(CheckersBoard board, bool forWhite);
    }

    public class Evaluator : IBoardEvaluator
    {
        public int EvaluateBoard(CheckersBoard board, bool forWhite)
        {
            int score = 0;
            int whitePieces = 0, blackPieces = 0;
            
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!board.IsDarkSquare(row, col)) continue;
                    
                    PieceType piece = board.GetPiece(row, col);
                    if (piece == PieceType.Empty) continue;

                    bool isWhite = PieceUtils.IsWhite(piece);
                    bool isKing = PieceUtils.IsKing(piece);
                    
                    if (isWhite) whitePieces++; else blackPieces++;
                    
                    int value = isKing ? 5 : 2;
                    int positionalBonus = 0;

                    if (!isKing)
                    {
                        int advancement = isWhite ? (7 - row) : row;
                        positionalBonus += advancement;

                        if ((isWhite && row == 7) || (!isWhite && row == 0))
                            positionalBonus += 2;
                    }

                    if (col == 0 || col == 7)
                        positionalBonus += 1;

                    int totalPieceValue = value + positionalBonus;
                    score += isWhite ? totalPieceValue : -totalPieceValue;
                }
            }

            //Console.WriteLine($"Board evaluation: White={whitePieces}, Black={blackPieces}, Score={score}, ForWhite={forWhite}");
            return forWhite ? score : -score;
        }
    }
}