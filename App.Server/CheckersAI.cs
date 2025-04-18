using System;
using System.Collections.Generic;
using System.Linq;
using App.Server;
using GrpcService;

public class CheckersAI
{
    private int max_depth;
    private int granulation;
    private bool isPerfomanceTest;

    public CheckersAI(int depth = 5, int granulation = 1, bool isPerformanceTest = false)
    {
        this.max_depth = depth;
        this.granulation = granulation;
        this.isPerfomanceTest = isPerformanceTest;
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, bool isWhiteTurn)
    {
        var mandatoryCaptures = GetMandatoryCaptures(board, isWhiteTurn);
        if (mandatoryCaptures.Count > 0)
        {
            return GetBestCapture(board, mandatoryCaptures, isWhiteTurn);
        }

        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int fromField, int toField) bestMove = (-1, -1);

        foreach (var move in GetAllValidMoves(board, isWhiteTurn))
        {
            var simulatedBoard = board.Clone();
            simulatedBoard.MovePiece(move.fromField, move.toField);

            int moveScore = Minimax(simulatedBoard, max_depth - 1, !isWhiteTurn);

            if ((isWhiteTurn && moveScore > bestScore) || (!isWhiteTurn && moveScore < bestScore))
            {
                bestScore = moveScore;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private (int fromField, int toField) GetBestCapture(CheckersBoard board, Dictionary<int, List<int>> captures, bool isWhiteTurn)
    {
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int fromField, int toField) bestCapture = (-1, -1);

        foreach (var fromField in captures.Keys)
        {
            foreach (var toField in captures[fromField])
            {
                var simulatedBoard = board.Clone();
                SimulateCapture(simulatedBoard, fromField, toField);

                int moveScore = Minimax(simulatedBoard, max_depth - 1, !isWhiteTurn);

                if ((isWhiteTurn && moveScore > bestScore) || (!isWhiteTurn && moveScore < bestScore))
                {
                    bestScore = moveScore;
                    bestCapture = (fromField, toField);
                }
            }
        }

        return bestCapture;
    }

    private void SimulateCapture(CheckersBoard board, int from, int to)
    {
        byte piece = board.GetField(from);
        var capture = board.GetValidCaptures(from).FirstOrDefault(c => c.Item1 == to);

        if (capture.Item1 == to)
        {
            board.SetField(from, (byte)PieceType.Empty);
            board.SetField(capture.Item2, (byte)PieceType.Empty);
            board.SetField(to, piece);

            // Promotion logic for both colors
            int promotionRow = IsWhitePiece(piece) ? 0 : 7; // White promotes at row 0, black at row 7
            if ((piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.BlackPawn) &&
                to / 4 == promotionRow)
            {
                board.SetField(to, (byte)(piece + 1)); // Promote to king (WhitePawn->WhiteKing, BlackPawn->BlackKing)
            }
        }
    }

    private int Minimax(CheckersBoard board, int depth, bool isMaximizingPlayer)
    {
        Console.WriteLine("DEPTH:" + depth);
        if (depth == 0 || IsGameOver(board))
        {
            return EvaluateBoard(board, isMaximizingPlayer);
        }

        var mandatoryCaptures = GetMandatoryCaptures(board, isMaximizingPlayer);
        var movesToConsider = mandatoryCaptures.Count > 0
            ? GetCaptureMoves(mandatoryCaptures)
            : GetAllValidMoves(board, isMaximizingPlayer);

        if (movesToConsider.Count == 0)
        {
            return isMaximizingPlayer ? int.MinValue : int.MaxValue;
        }

        if (isMaximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in movesToConsider)
            {
                var simulatedBoard = board.Clone();
                if (mandatoryCaptures.Count > 0)
                    SimulateCapture(simulatedBoard, move.fromField, move.toField);
                else
                    simulatedBoard.MovePiece(move.fromField, move.toField);

                int eval = Minimax(simulatedBoard, depth - 1, false);
                maxEval = Math.Max(maxEval, eval);
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in movesToConsider)
            {
                var simulatedBoard = board.Clone();
                if (mandatoryCaptures.Count > 0)
                    SimulateCapture(simulatedBoard, move.fromField, move.toField);
                else
                    simulatedBoard.MovePiece(move.fromField, move.toField);

                int eval = Minimax(simulatedBoard, depth - 1, true);
                minEval = Math.Min(minEval, eval);
            }
            return minEval;
        }
    }

    private int EvaluateBoard(CheckersBoard board, bool forWhite)
    {
        int score = 0;
        int whitePieces = 0;
        int blackPieces = 0;

        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            if (piece == (byte)PieceType.Empty) continue;

            bool isWhite = IsWhitePiece(piece);
            bool isKing = IsKingPiece(piece);
            int value = isKing ? 3 : 1;

            // Positional advantage - central control and advancement
            var (row, col) = GetBoardPosition(i);
            int centerDist = Math.Abs(col - 3) + Math.Abs(row - 3);
            int advancement = isWhite ? (7 - row) : row;

            if (isWhite)
            {
                whitePieces += value;
                score += value * granulation;
                score += advancement * granulation / 4;
                score -= centerDist * granulation / 6;
            }
            else
            {
                blackPieces += value;
                score -= value * granulation;
                score -= advancement * granulation / 4;
                score += centerDist * granulation / 6;
            }
        }

        // Endgame adjustments
        if (whitePieces + blackPieces < 10)
        {
            // Kings become more valuable in endgame
            score = score * 3 / 2;
        }

        return forWhite ? score : -score;
    }

    private (int row, int col) GetBoardPosition(int index)
    {
        int row = index / 4;
        int col = 2 * (index % 4) + (row % 2);
        return (row, col);
    }

    public bool IsGameOver(CheckersBoard board)
    {
        return !HasValidMoves(board, true) || !HasValidMoves(board, false);
    }

    public bool WhiteWon(CheckersBoard board)
    {
        return HasValidMoves(board, true) && !HasValidMoves(board, false);
    }

    private bool HasValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        if (GetMandatoryCaptures(board, isWhiteTurn).Count > 0) return true;

        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            if (IsColorPiece(piece, isWhiteTurn) && board.GetValidMoves(i).Count > 0)
                return true;
        }
        return false;
    }

    private Dictionary<int, List<int>> GetMandatoryCaptures(CheckersBoard board, bool isWhiteTurn)
    {
        var result = new Dictionary<int, List<int>>();

        for (int i = 0; i < 32; i++)
        {
            byte piece = board.GetField(i);
            if (!IsColorPiece(piece, isWhiteTurn)) continue;

            var captures = board.GetValidCaptures(i);
            if (captures.Count == 0) continue;

            var targetPositions = captures.Select(c => c.Item1).ToList();
            var multipleCaptures = board.GetMultipleCaptures(i);

            if (targetPositions.Count > 0 || multipleCaptures.Count > 0)
            {
                result[i] = targetPositions.Concat(multipleCaptures).ToList();
            }
        }
        return result;
    }

    private List<(int fromField, int toField)> GetAllValidMoves(CheckersBoard board, bool isWhiteTurn)
    {
        var moves = new List<(int, int)>();

        for (int i = 0; i < 32; i++)
        {
            if (!IsColorPiece(board.GetField(i), isWhiteTurn)) continue;
            moves.AddRange(board.GetValidMoves(i).Select(to => (i, to)));
        }

        return moves;
    }

    private List<(int fromField, int toField)> GetCaptureMoves(Dictionary<int, List<int>> captures)
    {
        return captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key, to))).ToList();
    }

    // Helper methods for piece identification
    private bool IsWhitePiece(byte piece)
    {
        return piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.WhiteKing;
    }

    private bool IsKingPiece(byte piece)
    {
        return piece == (byte)PieceType.WhiteKing || piece == (byte)PieceType.BlackKing;
    }

    private bool IsColorPiece(byte piece, bool isWhite)
    {
        return isWhite ? IsWhitePiece(piece) : !IsWhitePiece(piece) && piece != (byte)PieceType.Empty;
    }
}