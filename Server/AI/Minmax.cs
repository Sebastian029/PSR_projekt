using System;
using System.Collections.Generic;
using App.Server;

namespace MinimaxServer
{
    public class Minimax
    {
        private readonly int _maxDepth;
        private readonly IBoardEvaluator _evaluator;
        private int _nodesEvaluated = 0;

        public Minimax(int depth, IBoardEvaluator evaluator)
        {
            _maxDepth = depth;
            _evaluator = evaluator;
        }

        public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
        {
            _nodesEvaluated++;
            
            if (depth == 0 || new MoveGenerator().IsGameOver(board))
            {
                int evalScore = _evaluator.EvaluateBoard(board, isMaximizing);
                if (depth == _maxDepth)
                {
                    //Console.WriteLine($"Leaf node reached - depth: {depth}, score: {evalScore}, nodes evaluated: {_nodesEvaluated}");
                }
                return evalScore;
            }

            MoveGenerator generator = new MoveGenerator();
            var moves = generator.GetAllValidMoves(board, isMaximizing);

            if (depth == _maxDepth)
            {
                //Console.WriteLine($"Starting search at depth {depth}: Found {moves.Count} moves");
            }

            if (moves.Count == 0)
            {
                return _evaluator.EvaluateBoard(board, isMaximizing);
            }

            int bestEval = isMaximizing ? int.MinValue : int.MaxValue;

            foreach (var (fromRow, fromCol, toRow, toCol) in moves)
            {
                CheckersBoard simulated = board.Clone();
                simulated.MovePiece(fromRow, fromCol, toRow, toCol);

                int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
                bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
            }

            if (depth == _maxDepth)
            {
                //Console.WriteLine($"Search completed - Total nodes evaluated: {_nodesEvaluated}, Best score: {bestEval}");
            }

            return bestEval;
        }
    }
}
