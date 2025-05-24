// Minimax.cs
using App.Server;
using System;
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

    public Minimax(int depth, int granulationDepth, IBoardEvaluator evaluator, MinimaxDistributor distributor = null)
    {
        _maxDepth = depth;
        _evaluator = evaluator;
        _distributor = distributor;
        _granulationDepth = granulationDepth;
    }

    public (int fromRow, int fromCol, int toRow, int toCol) GetBestMove(CheckersBoard board, List<(int fromRow, int fromCol, int toRow, int toCol)> moves, bool isWhiteTurn)
    {
        if (moves == null || moves.Count == 0)
        {
            Console.WriteLine("No moves available");
            return (-1, -1, -1, -1);
        }

        Console.WriteLine($"GAME: Processing {moves.Count} moves at depth={_maxDepth}, granulation={_granulationDepth}");
        
        var tasks = new List<Task<int>>();
        var validMoves = new List<(int fromRow, int fromCol, int toRow, int toCol)>();

        // Walidacja i tworzenie zadań
        foreach (var move in moves)
        {
            try
            {
                // Walidacja współrzędnych
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

                // Test czy ruch jest możliwy
                var piece = simulated.GetPiece(move.fromRow, move.fromCol);
                if (piece == PieceType.Empty)
                {
                    Console.WriteLine($"No piece at ({move.fromRow},{move.fromCol})");
                    continue;
                }

                simulated.MovePiece(move.fromRow, move.fromCol, move.toRow, move.toCol);
                validMoves.Add(move);

                // Utwórz zadanie z zabezpieczeniem
                var task = Task.Run(() => {
                    try
                    {
                        var boardCopy = board.Clone();
                        boardCopy.MovePiece(move.fromRow, move.fromCol, move.toRow, move.toCol);
                        return MinimaxSearch(boardCopy, _maxDepth - 1, !isWhiteTurn, _granulationDepth);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in task for move ({move.fromRow},{move.fromCol}) to ({move.toRow},{move.toCol}): {ex.Message}");
                        return isWhiteTurn ? int.MinValue : int.MaxValue; // Najgorszy możliwy wynik
                    }
                });
                tasks.Add(task);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing move ({move.fromRow},{move.fromCol}) to ({move.toRow},{move.toCol}): {ex.Message}");
            }
        }

        if (tasks.Count == 0 || validMoves.Count == 0)
        {
            Console.WriteLine("No valid moves found");
            return (-1, -1, -1, -1);
        }

        try
        {
            Task.WaitAll(tasks.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error waiting for tasks: {ex.Message}");
            // Kontynuuj z dostępnymi wynikami
        }

        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int fromRow, int fromCol, int toRow, int toCol) bestMove = (-1, -1, -1, -1);
        
        for (int i = 0; i < Math.Min(tasks.Count, validMoves.Count); i++)
        {
            try
            {
                if (tasks[i].IsCompletedSuccessfully)
                {
                    int score = tasks[i].Result;
                    if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
                    {
                        bestScore = score;
                        bestMove = validMoves[i];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting result for move {i}: {ex.Message}");
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

    public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing, int granulationDepth)
    {
        try
        {
            if (depth == 0 || new MoveGenerator().IsGameOver(board))
                return _evaluator.EvaluateBoard(board, isMaximizing);

            int currentLayer = _maxDepth - depth;

            if (_distributor != null && currentLayer >= granulationDepth)
            {
                Console.WriteLine($"GAME: Distributing at Depth={depth}, Layer={currentLayer}, Granulation={granulationDepth}");
                return _distributor.DistributeMinimaxSearch(board, depth, isMaximizing);
            }

            MoveGenerator gen = new MoveGenerator();
            var caps = gen.GetMandatoryCaptures(board, isMaximizing);
            var allMoves = caps.Count > 0
                ? gen.GetCaptureMoves(caps)
                : gen.GetAllValidMoves(board, isMaximizing);

            if (allMoves.Count == 0)
            {
                return _evaluator.EvaluateBoard(board, isMaximizing);
            }

            var localTasks = new List<Task<int>>();
            
            foreach (var (fromRow, fromCol, toRow, toCol) in allMoves)
            {
                try
                {
                    CheckersBoard simulated = board.Clone();
                    if (simulated == null) continue;

                    if (caps.Count > 0)
                        new CaptureSimulator().SimulateCapture(simulated, fromRow, fromCol, toRow, toCol);
                    else
                        simulated.MovePiece(fromRow, fromCol, toRow, toCol);

                    localTasks.Add(Task.Run(() => {
                        try
                        {
                            return MinimaxSearch(simulated, depth - 1, !isMaximizing, granulationDepth);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in recursive minimax: {ex.Message}");
                            return isMaximizing ? int.MinValue : int.MaxValue;
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating task for move ({fromRow},{fromCol}) to ({toRow},{toCol}): {ex.Message}");
                }
            }

            if (localTasks.Count == 0)
            {
                return _evaluator.EvaluateBoard(board, isMaximizing);
            }

            try
            {
                Task.WaitAll(localTasks.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error waiting for local tasks: {ex.Message}");
            }

            int bestLocalEval = isMaximizing ? int.MinValue : int.MaxValue;
            foreach (var task in localTasks)
            {
                try
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        int eval = task.Result;
                        bestLocalEval = isMaximizing ? Math.Max(bestLocalEval, eval) : Math.Min(bestLocalEval, eval);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting task result: {ex.Message}");
                }
            }

            return bestLocalEval;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MinimaxSearch: {ex.Message}");
            return _evaluator.EvaluateBoard(board, isMaximizing);
        }
    }
}
