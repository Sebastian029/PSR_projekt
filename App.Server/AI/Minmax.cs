using App.Server;
using System.Collections.Generic;
using System.Linq;
using App.Client;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly IBoardEvaluator _evaluator;
    private readonly MinimaxDistributor _distributor;
    private readonly int _granulationDepth;

    public Minimax(int depth,int granulationDepth, IBoardEvaluator evaluator, MinimaxDistributor distributor = null)
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

        // If we should distribute and a distributor is available
        if (_distributor != null && depth > granulationDepth)
        {
            Console.WriteLine($"GAMEEEEEEEEEEE: Depth={depth}, Granulation={granulationDepth}");
            // Delegate the calculation to the distributor
            return _distributor.DistributeMinimaxSearch(board, depth, isMaximizing);
        }

        // Otherwise, calculate locally
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
