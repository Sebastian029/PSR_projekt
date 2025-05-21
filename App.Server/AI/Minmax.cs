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

    public (int fromField, int toField) GetBestMove(CheckersBoard board, List<(int from, int to)> moves, bool isWhiteTurn)
    {
        Console.WriteLine($"GAME: Processing moves at depth={_maxDepth}, granulation={_granulationDepth}");
        
        var tasks = new List<Task<int>>();
        foreach (var move in moves)
        {
            var simulated = board.Clone();
            simulated.MovePiece(move.from, move.to);
            
            // Start the evaluation in parallel
            tasks.Add(Task.Run(() => MinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn, _granulationDepth)));
        }
        
        // Wait for all evaluations to complete
        Task.WaitAll(tasks.ToArray());
        
        // Find the best move
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int from, int to) bestMove = (-1, -1);
        
        for (int i = 0; i < moves.Count; i++)
        {
            int score = tasks[i].Result;
            if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
            {
                bestScore = score;
                bestMove = moves[i];
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

        // Calculate current layer from depth
        int currentLayer = _maxDepth - depth;
        
        // If we've reached the granulation depth and have a distributor
        if (_distributor != null && currentLayer >= granulationDepth)
        {
            Console.WriteLine($"GAME: Distributing at Depth={depth}, Layer={currentLayer}, Granulation={granulationDepth}");
            
            // Directly distribute the board to the server
            // The server will generate moves and evaluate the position
            return _distributor.DistributeMinimaxSearch(board, depth, isMaximizing);
        }
        
        // Process locally for layers above granulation depth
        MoveGenerator gen = new MoveGenerator();
        var caps = gen.GetMandatoryCaptures(board, isMaximizing);
        var allMoves = caps.Count > 0
            ? gen.GetCaptureMoves(caps)
            : gen.GetAllValidMoves(board, isMaximizing);
        
        // Process moves in parallel locally
        var localTasks = new List<Task<int>>();
        
        foreach (var (from, to) in allMoves)
        {
            CheckersBoard simulated = board.Clone();
            
            if (caps.Count > 0)
                new CaptureSimulator().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);
            
            // Create a task for local processing
            localTasks.Add(Task.Run(() => MinimaxSearch(simulated, depth - 1, !isMaximizing, granulationDepth)));
        }
        
        // Wait for all local evaluations to complete
        Task.WaitAll(localTasks.ToArray());
        
        // Determine the best local result
        int bestLocalEval = isMaximizing ? int.MinValue : int.MaxValue;
        foreach (var task in localTasks)
        {
            int eval = task.Result;
            bestLocalEval = isMaximizing ? Math.Max(bestLocalEval, eval) : Math.Min(bestLocalEval, eval);
        }
        
        return bestLocalEval;
    }
}
