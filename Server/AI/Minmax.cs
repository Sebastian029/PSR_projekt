using App.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using App.Server;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly IBoardEvaluator _evaluator;

    public Minimax(int depth,IBoardEvaluator evaluator)
    {
        _maxDepth = depth;
        _evaluator = evaluator;
    }
    
    
    public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine("Depth:" + depth);
        
        if (depth == 0 || new MoveGenerator().IsGameOver(board))
            return _evaluator.EvaluateBoard(board, isMaximizing);
            
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
                
            int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
            bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
        }
        
        return bestEval;
    }
}
