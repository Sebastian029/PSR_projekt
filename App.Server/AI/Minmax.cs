using App.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using App.Client;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly IBoardEvaluator _evaluator;
    private readonly MinimaxDistributor _distributor;
    private readonly int _granulationDepth;

    public Minimax(int depth, int granulationDepth, IBoardEvaluator evaluator, MinimaxDistributor distributor = null)
    {
        _maxDepth = depth;
        _evaluator = evaluator;
        _distributor = distributor;
        _granulationDepth = granulationDepth;
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, List<(int from, int to)> moves, bool isWhiteTurn)
    {
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int from, int to) bestMove = (-1, -1);

        foreach (var move in moves)
        {
            var simulated = board.Clone();
            simulated.MovePiece(move.from, move.to);

            int score = MinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn, _granulationDepth);

            if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, Dictionary<int, List<int>> captures, bool isWhiteTurn)
    {
        var moves = captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key, to))).ToList();
        return GetBestMove(board, moves, isWhiteTurn);
    }
    
    public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing, int granulationDepth)
    {
        if (depth == 0 || new MoveGenerator().IsGameOver(board))
            return _evaluator.EvaluateBoard(board, isMaximizing);

        // If we've reached the granulation depth and have a distributor, handle all subtrees in parallel
        if (_distributor != null && depth == _maxDepth - granulationDepth)
        {
            Console.WriteLine($"GAME: Distributing at Depth={depth}, Granulation={granulationDepth}");
            // At this level, we'll collect all possible moves and distribute them all at once
            return _distributor.DistributeMinimaxSearch(board, depth, isMaximizing);
        }

        // Standard minimax logic for depths above granulation or when no distributor is available
        MoveGenerator generator = new MoveGenerator();
        var captures = generator.GetMandatoryCaptures(board, isMaximizing);
        var moves = captures.Count > 0
            ? generator.GetCaptureMoves(captures)
            : generator.GetAllValidMoves(board, isMaximizing);

        int bestEval = isMaximizing ? int.MinValue : int.MaxValue;

        foreach (var (from, to) in moves)
        {
            CheckersBoard simulated = board.Clone();

            if (captures.Count > 0)
                new CaptureSimulator().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing, granulationDepth);
            bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
        }

        return bestEval;
    }

}
