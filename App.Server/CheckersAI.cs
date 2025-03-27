using System;
using System.Collections.Generic;
using System.Linq;
using App.Server;

public class CheckersAI
{
    private const int MAX_DEPTH = 5;

    public (int fromField, int toField) GetBestMove(CheckersBoard board, bool isWhiteTurn)
    {
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        int bestFromField = -1;
        int bestToField = -1;

        var validMoves = GetAllValidMoves(board, isWhiteTurn);

        foreach (var move in validMoves)
        {
            var simulatedBoard = board.Clone();
            simulatedBoard.MovePiece(move.fromField, move.toField);

            int moveScore = Minimax(simulatedBoard, MAX_DEPTH - 1, !isWhiteTurn, int.MinValue, int.MaxValue);

            if (isWhiteTurn && moveScore > bestScore)
            {
                bestScore = moveScore;
                bestFromField = move.fromField;
                bestToField = move.toField;
            }
            else if (!isWhiteTurn && moveScore < bestScore)
            {
                bestScore = moveScore;
                bestFromField = move.fromField;
                bestToField = move.toField;
            }
        }

        return (bestFromField, bestToField);
    }

    private int Minimax(CheckersBoard board, int depth, bool isMaximizingPlayer, int alpha, int beta)
    {
        if (depth == 0 || IsGameOver(board))
        {
            return EvaluateBoard(board);
        }

        var validMoves = GetAllValidMoves(board, isMaximizingPlayer);

        if (validMoves.Count == 0)
        {
            return isMaximizingPlayer ? int.MinValue : int.MaxValue;
        }

        if (isMaximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in validMoves)
            {
                var simulatedBoard = board.Clone();
                simulatedBoard.MovePiece(move.fromField, move.toField);

                int eval = Minimax(simulatedBoard, depth - 1, false, alpha, beta);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                    break; 
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in validMoves)
            {
                var simulatedBoard = board.Clone();
                simulatedBoard.MovePiece(move.fromField, move.toField);

                int eval = Minimax(simulatedBoard, depth - 1, true, alpha, beta);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                    break; 
            }
            return minEval;
        }
    }

    private int EvaluateBoard(CheckersBoard board)
    {
        int score = 0;
    
        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            switch (piece)
            {
                case (byte)PieceType.WhitePawn:
                    score += 1;
                    break;
      
                case (byte)PieceType.BlackPawn:
                    score -= 1;
                    break;
         
            }
        }

        return score;
    }

    private bool IsGameOver(CheckersBoard board)
    {
        // Check if either side has no pieces or no valid moves
        bool whiteHasMoves = HasValidMoves(board, true);
        bool blackHasMoves = HasValidMoves(board, false);

        return !whiteHasMoves || !blackHasMoves;
    }

    private bool HasValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            
            // Skip if piece doesn't match current turn
            if (isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing))
            {
                var moves = board.GetValidMoves(i);
                var captures = board.GetValidCaptures(i);
                if (moves.Count > 0 || captures.Count > 0)
                    return true;
            }
            else if (!isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing))
            {
                var moves = board.GetValidMoves(i);
                var captures = board.GetValidCaptures(i);
                if (moves.Count > 0 || captures.Count > 0)
                    return true;
            }
        }
        return false;
    }

    private List<(int fromField, int toField)> GetAllValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        var moves = new List<(int fromField, int toField)>();

        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            
            // Skip if piece doesn't match current turn
            if (isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing))
                continue;
            if (!isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing))
                continue;

            // Get valid moves and captures
            var validMoves = board.GetValidMoves(i);
            var validCaptures = board.GetValidCaptures(i);

            // Add moves
            moves.AddRange(validMoves.Select(to => (i, to)));
            moves.AddRange(validCaptures.Select(to => (i, to)));
        }

        return moves;
    }
}