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
                // Parallelize top-level move evaluation
                if (moves.Count == 0)
                {
                    return _evaluator.EvaluateBoard(board, isMaximizing);
                }

                int bestEval = isMaximizing ? int.MinValue : int.MaxValue;
                object lockObj = new object();

                System.Threading.Tasks.Parallel.ForEach(moves, move =>
                {
                    var (fromRow, fromCol, toRow, toCol) = move;
                    CheckersBoard simulated = board.Clone();
                    simulated.MovePiece(fromRow, fromCol, toRow, toCol);
                    int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);

                    lock (lockObj)
                    {
                        if (isMaximizing)
                            bestEval = Math.Max(bestEval, eval);
                        else
                            bestEval = Math.Min(bestEval, eval);
                    }
                });

                //Console.WriteLine($"Search completed - Total nodes evaluated: {_nodesEvaluated}, Best score: {bestEval}");
                return bestEval;
            }

            if (moves.Count == 0)
            {
                return _evaluator.EvaluateBoard(board, isMaximizing);
            }

            int bestEvalSeq = isMaximizing ? int.MinValue : int.MaxValue;
            foreach (var (fromRow, fromCol, toRow, toCol) in moves)
            {
                CheckersBoard simulated = board.Clone();
                simulated.MovePiece(fromRow, fromCol, toRow, toCol);

                int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
                bestEvalSeq = isMaximizing ? Math.Max(bestEvalSeq, eval) : Math.Min(bestEvalSeq, eval);
            }

            return bestEvalSeq;
        }
    }
}
