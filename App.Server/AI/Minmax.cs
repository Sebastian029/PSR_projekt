using App.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Client;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly IBoardEvaluator _evaluator;
    private readonly MinimaxDistributor _distributor;
    private readonly int _granulationDepth;
    private readonly int _parallelThreshold;

    public Minimax(int depth, int granulationDepth, IBoardEvaluator evaluator, MinimaxDistributor distributor = null, int parallelThreshold = 4)
    {
        _maxDepth = depth;
        _evaluator = evaluator;
        _distributor = distributor;
        _granulationDepth = granulationDepth;
        _parallelThreshold = parallelThreshold;
    }

    public (int fromRow, int fromCol, int toRow, int toCol) GetBestMove(CheckersBoard board, List<(int fromRow, int fromCol, int toRow, int toCol)> moves, bool isWhiteTurn)
    {
        if (moves == null || moves.Count == 0)
        {
            Console.WriteLine("No moves available");
            return (-1, -1, -1, -1);
        }

        Console.WriteLine($"GAME: Processing {moves.Count} moves at depth={_maxDepth}, granulation={_granulationDepth}");
        
        var validMoves = new List<(int fromRow, int fromCol, int toRow, int toCol)>();
        var initialTasks = new List<(CheckersBoard, int, bool, List<(int fromRow, int fromCol, int toRow, int toCol)>)>();

        // Prepare and validate moves
        foreach (var move in moves)
        {
            try
            {
                // Validate coordinates
                if (move.fromRow < 0 || move.fromRow >= 8 || move.fromCol < 0 || move.fromCol >= 8 ||
                    move.toRow < 0 || move.toRow >= 8 || move.toCol < 0 || move.toCol >= 8)
                {
                    Console.WriteLine($"Skipping invalid move: ({move.fromRow},{move.fromCol}) to ({move.toRow},{move.toCol})");
                    continue;
                }

                var simulated = board.Clone();
                if (simulated == null)
                {
                    Console.WriteLine("Board clone failed");
                    continue;
                }

                // Test if move is possible
                var piece = simulated.GetPiece(move.fromRow, move.fromCol);
                if (piece == PieceType.Empty)
                {
                    Console.WriteLine($"No piece at ({move.fromRow},{move.fromCol})");
                    continue;
                }

                simulated.MovePiece(move.fromRow, move.fromCol, move.toRow, move.toCol);
                validMoves.Add(move);
                initialTasks.Add((simulated, _maxDepth - 1, !isWhiteTurn, new List<(int, int, int, int)>{ move }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing move ({move.fromRow},{move.fromCol}) to ({move.toRow},{move.toCol}): {ex.Message}");
            }
        }

        if (initialTasks.Count == 0 || validMoves.Count == 0)
        {
            Console.WriteLine("No valid moves found");
            return (-1, -1, -1, -1);
        }

        // Granulation logic: expand (granulationDepth-1) layers locally
        var tasks = new List<(CheckersBoard, int, bool, List<(int fromRow, int fromCol, int toRow, int toCol)>)>(initialTasks);
        for (int g = 1; g < _granulationDepth; g++)
        {
            var nextTasks = new List<(CheckersBoard, int, bool, List<(int, int, int, int)>)>();
            foreach (var (b, d, maximizing, path) in tasks)
            {
                if (d == 0 || new MoveGenerator().IsGameOver(b))
                {
                    // No further expansion
                    nextTasks.Add((b, d, maximizing, path));
                    continue;
                }
                var (movesToExpand, isCapture) = GetAllValidMovesForBoard(b, maximizing);
                if (movesToExpand.Count == 0)
                {
                    nextTasks.Add((b, d, maximizing, path));
                    continue;
                }
                foreach (var (fromRow, fromCol, toRow, toCol) in movesToExpand)
                {
                    var sim = b.Clone();
                    if (sim == null) continue;
                    if (isCapture)
                        new CaptureSimulator().SimulateCapture(sim, fromRow, fromCol, toRow, toCol);
                    else
                        sim.MovePiece(fromRow, fromCol, toRow, toCol);
                    var newPath = new List<(int, int, int, int)>(path) { (fromRow, fromCol, toRow, toCol) };
                    nextTasks.Add((sim, d - 1, !maximizing, newPath));
                }
            }
            tasks = nextTasks;
        }

        // Now distribute all resulting tasks
        var distributedTasks = tasks.Select(t => (t.Item1, t.Item2, t.Item3)).ToList();
        // For mapping results back to top-level moves
        var moveIndexMap = new List<int>();
        for (int i = 0; i < tasks.Count; i++)
        {
            // The first move in the path is the top-level move
            var topMove = tasks[i].Item4[0];
            int idx = validMoves.FindIndex(m => m.Equals(topMove));
            moveIndexMap.Add(idx);
        }

        List<int> results = ProcessMovesWithParallelDistribution(distributedTasks, isWhiteTurn);

        // Aggregate results for each top-level move
        var moveScores = new int[validMoves.Count];
        for (int i = 0; i < moveScores.Length; i++)
            moveScores[i] = isWhiteTurn ? int.MinValue : int.MaxValue;
        for (int i = 0; i < results.Count; i++)
        {
            int idx = moveIndexMap[i];
            if (idx < 0 || idx >= moveScores.Length) continue;
            if (isWhiteTurn)
                moveScores[idx] = Math.Max(moveScores[idx], results[i]);
            else
                moveScores[idx] = Math.Min(moveScores[idx], results[i]);
        }

        // Find best move
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int fromRow, int fromCol, int toRow, int toCol) bestMove = (-1, -1, -1, -1);
        for (int i = 0; i < moveScores.Length; i++)
        {
            int score = moveScores[i];
            if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
            {
                bestScore = score;
                bestMove = validMoves[i];
            }
        }

        Console.WriteLine($"Best move: ({bestMove.fromRow},{bestMove.fromCol}) to ({bestMove.toRow},{bestMove.toCol}) with score {bestScore}");
        return bestMove;
    }

    public (int fromRow, int fromCol, int toRow, int toCol) GetBestMove(CheckersBoard board, Dictionary<(int row, int col), List<(int row, int col)>> captures, bool isWhiteTurn)
    {
        var moves = captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key.row, kvp.Key.col, to.row, to.col))).ToList();
        return GetBestMove(board, moves, isWhiteTurn);
    }

    private List<int> ProcessMovesWithParallelDistribution(List<(CheckersBoard board, int depth, bool isMaximizing)> tasks, bool isWhiteTurn)
    {
        try
        {
            if (_distributor != null && tasks.Count >= _parallelThreshold)
            {
                Console.WriteLine($"GAME: Using distributed processing for {tasks.Count} tasks");
                var resultTask = _distributor.ProcessTasksLoadBalanced(tasks);
                resultTask.Wait();
                return (List<int>)resultTask.Result;
            }
            else
            {
                Console.WriteLine($"GAME: Using parallel local processing for {tasks.Count} tasks");
                return ProcessMovesWithParallelLocal(tasks, isWhiteTurn);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in distributed processing: {ex.Message}");
            return ProcessMovesWithParallelLocal(tasks, isWhiteTurn);
        }
    }

    private List<int> ProcessMovesWithParallelLocal(List<(CheckersBoard board, int depth, bool isMaximizing)> tasks, bool isWhiteTurn)
    {
        var results = new ConcurrentBag<(int index, int score)>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        try
        {
            Parallel.For(0, tasks.Count, parallelOptions, i =>
            {
                try
                {
                    var task = tasks[i];
                    int score = MinimaxSearch(task.board, task.depth, task.isMaximizing, _granulationDepth);
                    results.Add((i, score));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in parallel local task {i}: {ex.Message}");
                    results.Add((i, isWhiteTurn ? int.MinValue : int.MaxValue));
                }
            });

            // Convert concurrent bag to ordered list
            var orderedResults = results.OrderBy(r => r.index).Select(r => r.score).ToList();
            
            // Fill any missing results
            while (orderedResults.Count < tasks.Count)
            {
                orderedResults.Add(isWhiteTurn ? int.MinValue : int.MaxValue);
            }

            return orderedResults;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in parallel processing: {ex.Message}");
            // Fallback to sequential processing
            return ProcessMovesSequentially(tasks, isWhiteTurn);
        }
    }

    private List<int> ProcessMovesSequentially(List<(CheckersBoard board, int depth, bool isMaximizing)> tasks, bool isWhiteTurn)
    {
        var results = new List<int>();
        
        foreach (var task in tasks)
        {
            try
            {
                int score = MinimaxSearch(task.board, task.depth, task.isMaximizing, _granulationDepth);
                results.Add(score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in sequential task processing: {ex.Message}");
                results.Add(isWhiteTurn ? int.MinValue : int.MaxValue);
            }
        }
        
        return results;
    }

    public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing, int granulationDepth)
    {
        try
        {
            if (depth == 0 || new MoveGenerator().IsGameOver(board))
                return _evaluator.EvaluateBoard(board, isMaximizing);

            int currentLayer = _maxDepth - depth;

            // Use distributed processing at specified granulation level
            if (_distributor != null && currentLayer >= granulationDepth)
            {
                return ProcessLayerWithDistribution(board, depth, isMaximizing, granulationDepth);
            }

            // Use parallel local processing for deeper levels
            return ProcessLayerWithParallelLocal(board, depth, isMaximizing, granulationDepth);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MinimaxSearch: {ex.Message}");
            return _evaluator.EvaluateBoard(board, isMaximizing);
        }
    }

    private int ProcessLayerWithDistribution(CheckersBoard board, int depth, bool isMaximizing, int granulationDepth)
    {
        try
        {
            Console.WriteLine($"GAME: Distributing at Depth={depth}, Layer={_maxDepth - depth}, Granulation={granulationDepth}");
            
            var (allMoves, isCapture) = GetAllValidMovesForBoard(board, isMaximizing);
            
            if (allMoves.Count == 0)
                return _evaluator.EvaluateBoard(board, isMaximizing);

            // Create distributed tasks for ALL moves at this level
            var distributedTasks = new List<(CheckersBoard, int, bool)>();
            
            foreach (var (fromRow, fromCol, toRow, toCol) in allMoves)
            {
                try
                {
                    CheckersBoard simulated = board.Clone();
                    if (simulated == null) continue;

                    if (isCapture)
                        new CaptureSimulator().SimulateCapture(simulated, fromRow, fromCol, toRow, toCol);
                    else
                        simulated.MovePiece(fromRow, fromCol, toRow, toCol);

                    distributedTasks.Add((simulated, depth - 1, !isMaximizing));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating distributed task: {ex.Message}");
                }
            }

            if (distributedTasks.Count == 0)
                return _evaluator.EvaluateBoard(board, isMaximizing);

            // Use distributor for ALL moves
            var resultTask = _distributor.ProcessTasksLoadBalanced(distributedTasks);
            resultTask.Wait();
            var results = resultTask.Result;
            
            return isMaximizing ? results.Max() : results.Min();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in distributed layer processing: {ex.Message}");
            return _evaluator.EvaluateBoard(board, isMaximizing);
        }
    }

    private int ProcessLayerWithParallelLocal(CheckersBoard board, int depth, bool isMaximizing, int granulationDepth)
    {
        try
        {
            var (allMoves, isCapture) = GetAllValidMovesForBoard(board, isMaximizing);

            if (allMoves.Count == 0)
                return _evaluator.EvaluateBoard(board, isMaximizing);

            // Use parallel processing for multiple moves
            if (allMoves.Count >= _parallelThreshold)
            {
                var results = new ConcurrentBag<int>();
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(allMoves, parallelOptions, move =>
                {
                    try
                    {
                        CheckersBoard simulated = board.Clone();
                        if (simulated != null)
                        {
                            if (isCapture)
                                new CaptureSimulator().SimulateCapture(simulated, move.fromRow, move.fromCol, move.toRow, move.toCol);
                            else
                                simulated.MovePiece(move.fromRow, move.fromCol, move.toRow, move.toCol);

                            int score = MinimaxSearch(simulated, depth - 1, !isMaximizing, granulationDepth);
                            results.Add(score);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in parallel move processing: {ex.Message}");
                        results.Add(isMaximizing ? int.MinValue : int.MaxValue);
                    }
                });

                var resultList = results.ToList();
                return resultList.Count > 0 ? (isMaximizing ? resultList.Max() : resultList.Min()) : _evaluator.EvaluateBoard(board, isMaximizing);
            }
            else
            {
                // Sequential processing for small number of moves
                int bestEval = isMaximizing ? int.MinValue : int.MaxValue;
                
                foreach (var (fromRow, fromCol, toRow, toCol) in allMoves)
                {
                    try
                    {
                        CheckersBoard simulated = board.Clone();
                        if (simulated == null) continue;

                        if (isCapture)
                            new CaptureSimulator().SimulateCapture(simulated, fromRow, fromCol, toRow, toCol);
                        else
                            simulated.MovePiece(fromRow, fromCol, toRow, toCol);

                        int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing, granulationDepth);
                        bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing move ({fromRow},{fromCol}) to ({toRow},{toCol}): {ex.Message}");
                    }
                }

                return bestEval;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in parallel local layer processing: {ex.Message}");
            return _evaluator.EvaluateBoard(board, isMaximizing);
        }
    }

    private (List<(int fromRow, int fromCol, int toRow, int toCol)> moves, bool isCapture) GetAllValidMovesForBoard(CheckersBoard board, bool isMaximizing)
    {
        try
        {
            MoveGenerator gen = new MoveGenerator();
            var caps = gen.GetMandatoryCaptures(board, isMaximizing);
            
            if (caps.Count > 0)
            {
                return (gen.GetCaptureMoves(caps), true);
            }
            else
            {
                return (gen.GetAllValidMoves(board, isMaximizing), false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting valid moves: {ex.Message}");
            return (new List<(int, int, int, int)>(), false);
        }
    }
}