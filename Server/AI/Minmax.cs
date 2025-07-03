using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using App.Server;

namespace MinimaxServer
{
    public class Minimax
    {
        private readonly int _maxDepth;
        private readonly IBoardEvaluator _evaluator;
        private int _nodesEvaluated = 0;
        private readonly ParallelOptions _parallelOptions;

        public Minimax(int depth, IBoardEvaluator evaluator)
        {
            _maxDepth = depth;
            _evaluator = evaluator;
            _parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
        }

        public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
        {
            _nodesEvaluated++;
            
            if (depth == 0 || new MoveGenerator().IsGameOver(board))
            {
                int evalScore = _evaluator.EvaluateBoard(board, isMaximizing);
                if (depth == _maxDepth)
                {
                }
                return evalScore;
            }

            var generator = new MoveGenerator();
            var moves = generator.GetAllValidMoves(board, isMaximizing);

            if (depth == _maxDepth)
            {
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

                return bestEval;
            }

            if (moves.Count == 0)
            {
                return _evaluator.EvaluateBoard(board, isMaximizing);
            }

            if (moves.Count >= 2) 
            {
                var results = new int[moves.Count];
                var moveArray = moves.ToArray();

                Parallel.For(0, moves.Count, _parallelOptions, i =>
                {
                    var (fromRow, fromCol, toRow, toCol) = moveArray[i];
                    var simulated = board.Clone();
                    simulated.MovePiece(fromRow, fromCol, toRow, toCol);
                    results[i] = MinimaxSearch(simulated, depth - 1, !isMaximizing);
                });

                return isMaximizing ? results.Max() : results.Min();
            }
            else
            {
                int bestEval = isMaximizing ? int.MinValue : int.MaxValue;
                
                foreach (var (fromRow, fromCol, toRow, toCol) in moves)
                {
                    var simulated = board.Clone();
                    simulated.MovePiece(fromRow, fromCol, toRow, toCol);
                    int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
                    bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
                }

                return bestEval;
            }
        }
    }
}
