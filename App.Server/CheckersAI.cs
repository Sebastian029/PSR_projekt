using System;
using System.Collections.Generic;
using System.Linq;
using App.Server;
using GrpcService;

public class CheckersAI
{
    private int MAX_DEPTH = 5;
    private int GRANULATION = 1; // Default granulation value
    private bool IS_PERFORMANCE_TEST = false;

    public CheckersAI(int depth = 5, int granulation = 1, bool isPerformanceTest = false)
    {
        MAX_DEPTH = depth;
        GRANULATION = granulation;
        IS_PERFORMANCE_TEST = isPerformanceTest;
    }

    public void UpdateSettings(int depth, int granulation, bool isPerformanceTest)
    {
        MAX_DEPTH = depth;
        GRANULATION = granulation;
        IS_PERFORMANCE_TEST = isPerformanceTest;
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, bool isWhiteTurn)
    {
        // Check if any captures are mandatory
        var mandatoryCaptures = GetMandatoryCaptures(board, isWhiteTurn);
        if (mandatoryCaptures.Count > 0)
        {
            // If there are mandatory captures, choose the best one
            return GetBestCapture(board, mandatoryCaptures, isWhiteTurn);
        }

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

    private (int fromField, int toField) GetBestCapture(CheckersBoard board, Dictionary<int, List<int>> captures, bool isWhiteTurn)
    {
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        int bestFromField = -1;
        int bestToField = -1;

        foreach (var fromField in captures.Keys)
        {
            foreach (var toField in captures[fromField])
            {
                var simulatedBoard = board.Clone();
                // Need to simulate the capture properly
                SimulateCapture(simulatedBoard, fromField, toField);

                int moveScore = Minimax(simulatedBoard, MAX_DEPTH - 1, !isWhiteTurn, int.MinValue, int.MaxValue);

                if (isWhiteTurn && moveScore > bestScore)
                {
                    bestScore = moveScore;
                    bestFromField = fromField;
                    bestToField = toField;
                }
                else if (!isWhiteTurn && moveScore < bestScore)
                {
                    bestScore = moveScore;
                    bestFromField = fromField;
                    bestToField = toField;
                }
            }
        }

        return (bestFromField, bestToField);
    }

    private void SimulateCapture(CheckersBoard board, int from, int to)
    {
        byte piece = board.GetField(from);
        
        // Find the captured piece
        List<(int, int)> validCaptures = board.GetValidCaptures(from);
        int capturedPieceIndex = -1;
        
        foreach (var capture in validCaptures)
        {
            if (capture.Item1 == to)
            {
                capturedPieceIndex = capture.Item2;
                break;
            }
        }

        if (capturedPieceIndex != -1)
        {
            // Perform the capture
            board.SetField(from, (byte)PieceType.Empty);
            board.SetField(capturedPieceIndex, (byte)PieceType.Empty);
            board.SetField(to, piece);

            // Check for promotion to king
            if (piece == (byte)PieceType.WhitePawn && to < 4)
            {
                board.SetField(to, (byte)PieceType.WhiteKing);
            }
            else if (piece == (byte)PieceType.BlackPawn && to >= 28)
            {
                board.SetField(to, (byte)PieceType.BlackKing);
            }
        }
    }

    private int Minimax(CheckersBoard board, int depth, bool isMaximizingPlayer, int alpha, int beta)
    {
        if (depth == 0 || IsGameOver(board))
        {
            return EvaluateBoard(board);
        }

        // Check if there are mandatory captures
        var mandatoryCaptures = GetMandatoryCaptures(board, isMaximizingPlayer);
        
        if (mandatoryCaptures.Count > 0)
        {
            // If there are mandatory captures, only consider them
            if (isMaximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (var fromField in mandatoryCaptures.Keys)
                {
                    foreach (var toField in mandatoryCaptures[fromField])
                    {
                        var simulatedBoard = board.Clone();
                        SimulateCapture(simulatedBoard, fromField, toField);

                        int eval = Minimax(simulatedBoard, depth - 1, false, alpha, beta);
                        maxEval = Math.Max(maxEval, eval);
                        alpha = Math.Max(alpha, eval);

                        if (beta <= alpha)
                            break;
                    }
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var fromField in mandatoryCaptures.Keys)
                {
                    foreach (var toField in mandatoryCaptures[fromField])
                    {
                        var simulatedBoard = board.Clone();
                        SimulateCapture(simulatedBoard, fromField, toField);

                        int eval = Minimax(simulatedBoard, depth - 1, true, alpha, beta);
                        minEval = Math.Min(minEval, eval);
                        beta = Math.Min(beta, eval);

                        if (beta <= alpha)
                            break;
                    }
                }
                return minEval;
            }
        }
        else
        {
            // If no mandatory captures, consider all valid moves
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
                    score += 1 * GRANULATION;
                    // Bonus for advancing pawns
                    score += (7 - i / 4) * GRANULATION / 4;
                    break;
                
                case (byte)PieceType.WhiteKing:
                    score += 3 * GRANULATION;
                    // Kings are more valuable in the center
                    int whiteKingRowCol = GetRowCol(i);
                    int whiteKingDistFromCenter = Math.Abs(whiteKingRowCol / 10 - 3) + Math.Abs(whiteKingRowCol % 10 - 3);
                    score += (6 - whiteKingDistFromCenter) * GRANULATION / 3;
                    break;
                
                case (byte)PieceType.BlackPawn:
                    score -= 1 * GRANULATION;
                    // Bonus for advancing pawns
                    score -= (i / 4) * GRANULATION / 4;
                    break;
                
                case (byte)PieceType.BlackKing:
                    score -= 3 * GRANULATION;
                    // Kings are more valuable in the center
                    int blackKingRowCol = GetRowCol(i);
                    int blackKingDistFromCenter = Math.Abs(blackKingRowCol / 10 - 3) + Math.Abs(blackKingRowCol % 10 - 3);
                    score -= (6 - blackKingDistFromCenter) * GRANULATION / 3;
                    break;
            }
        }

        return score;
    }

    private int GetRowCol(int index)
    {
        int row = index / 4;
        int col = 2 * (index % 4) + (row % 2);
        return row * 10 + col;
    }

    public bool IsGameOver(CheckersBoard board)
    {
        // Check if either side has no pieces or no valid moves
        bool whiteHasMoves = HasValidMoves(board, true);
        bool blackHasMoves = HasValidMoves(board, false);

        return !whiteHasMoves || !blackHasMoves;
    }

    public bool WhiteWon(CheckersBoard board)
    {
        bool whiteHasMoves = HasValidMoves(board, true);
        return whiteHasMoves;
    }
    
    private bool HasValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        var mandatoryCaptures = GetMandatoryCaptures(board, isWhiteTurn);
        if (mandatoryCaptures.Count > 0)
            return true;

        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            
            if (isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing))
            {
                var moves = board.GetValidMoves(i);
                if (moves.Count > 0)
                    return true;
            }
            else if (!isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing))
            {
                var moves = board.GetValidMoves(i);
                if (moves.Count > 0)
                    return true;
            }
        }
        return false;
    }

    private Dictionary<int, List<int>> GetMandatoryCaptures(CheckersBoard board, bool isWhiteTurn)
    {
        var result = new Dictionary<int, List<int>>();
        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            if ((isWhiteTurn && (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing)) ||
                (!isWhiteTurn && (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.BlackKing)))
            {
                var captures = board.GetValidCaptures(i);
                
                // Extract just the target positions from the captures
                var targetPositions = new List<int>();
                foreach (var capture in captures)
                {
                    targetPositions.Add(capture.Item1);
                }
                
                var multipleCaptures = board.GetMultipleCaptures(i);
                
                if (targetPositions.Count > 0 || multipleCaptures.Count > 0)
                {
                    var allCaptures = new List<int>();
                    
                    allCaptures.AddRange(targetPositions);
                    allCaptures.AddRange(multipleCaptures);
                    result[i] = allCaptures;
                }
            }
        }
        return result;
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

            // Get valid moves
            var validMoves = board.GetValidMoves(i);
            
            // Add moves
            moves.AddRange(validMoves.Select(to => (i, to)));
        }

        return moves;
    }
}