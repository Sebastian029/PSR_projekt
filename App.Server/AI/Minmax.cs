using App.Server;
using Grpc.Core;
using GrpcServer;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly int _granulationDepth;  // Defines how many layers to calculate locally
    private readonly IBoardEvaluator _evaluator;
    private readonly CheckersService.CheckersServiceClient _grpcClient;

    public Minimax(int depth, int granulationDepth, IBoardEvaluator evaluator, ChannelBase grpcChannel = null)
    {
        _maxDepth = depth;
        _granulationDepth = granulationDepth;
        _evaluator = evaluator;
        _grpcClient = grpcChannel != null ? new CheckersService.CheckersServiceClient(grpcChannel) : null;
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, List<(int from, int to)> moves, bool isWhiteTurn)
    {
        // Start with local granulation
        var result = ProcessMovesLocally(board, moves, null, _maxDepth, isWhiteTurn, 0);
        return (result.from, result.to);
    }

    public (int fromField, int toField) GetBestCapture(CheckersBoard board, Dictionary<int, List<int>> captures, bool isWhiteTurn)
    {
        // Convert captures dictionary to a list of moves
        var moves = captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key, to))).ToList();
        
        // Use the same evaluation logic but indicate these are capture moves
        var result = ProcessMovesLocally(board, moves, captures, _maxDepth, isWhiteTurn, 0);
        return (result.from, result.to);
    }

    private (int value, int from, int to) ProcessMovesLocally(
        CheckersBoard board, 
        List<(int from, int to)> moves, 
        Dictionary<int, List<int>> captures, 
        int depth, 
        bool isWhiteTurn,
        int currentLayer)
    {
        // First layer of granulation is always local
        var tasks = moves.Select(move => Task.Run(() => {
            var (from, to) = move;
            var simulated = board.Clone();
            
            if (captures != null)
                new CaptureSimulator().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);
                
            // Calculate score based on current layer and granulation depth
            int score;
            
            // If we've processed the specified number of layers locally, distribute the rest
            if (_grpcClient != null && currentLayer >= _granulationDepth - 1 && depth > 1) 
            {
                // Distribute to remote servers
                score = DistributeCalculation(simulated, depth - 1, !isWhiteTurn).Result;
            }
            else if (depth > 1) 
            {
                // Continue locally with next layer
                var generator = new MoveGenerator();
                var nextCaptures = generator.GetMandatoryCaptures(simulated, !isWhiteTurn);
                var nextMoves = nextCaptures.Count > 0
                    ? generator.GetCaptureMoves(nextCaptures)
                    : generator.GetAllValidMoves(simulated, !isWhiteTurn);
                    
                if (nextMoves.Count > 0)
                {
                    var result = ProcessMovesLocally(
                        simulated, 
                        nextMoves, 
                        nextCaptures.Count > 0 ? nextCaptures : null, 
                        depth - 1, 
                        !isWhiteTurn,
                        currentLayer + 1);
                    score = result.value;
                }
                else
                {
                    // Game over or no moves available
                    score = _evaluator.EvaluateBoard(simulated, isWhiteTurn);
                }
            }
            else 
            {
                // Reached maximum depth, evaluate board
                score = _evaluator.EvaluateBoard(simulated, isWhiteTurn);
            }
            
            return (value: score, from, to);
        })).ToArray();
        
        Task.WaitAll(tasks);
        var results = tasks.Select(t => t.Result).ToList();
        
        if (isWhiteTurn)
            return results.OrderByDescending(r => r.value).First();
        else
            return results.OrderBy(r => r.value).First();
    }

    private int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine("SERVER DEPTH: " +depth);
        if (depth == 0 || new MoveGenerator().IsGameOver(board))
            return _evaluator.EvaluateBoard(board, isMaximizing);

        var generator = new MoveGenerator();
        var captures = generator.GetMandatoryCaptures(board, isMaximizing);
        var moves = captures.Count > 0
            ? generator.GetCaptureMoves(captures)
            : generator.GetAllValidMoves(board, isMaximizing);

        int bestEval = isMaximizing ? int.MinValue : int.MaxValue;

        foreach (var (from, to) in moves)
        {
            var simulated = board.Clone();

            if (captures.Count > 0)
                new CaptureSimulator().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
            bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
        }

        return bestEval;
    }
    private async Task<int> DistributeCalculation(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine($"[REMOTE] Distributing calculation: Depth={depth}");
        var request = new BoardStateRequest
        {
            BoardState = { board.board },
            IsWhiteTurn = isMaximizing,
            Depth = depth,
            ClientStartTicks = DateTime.UtcNow.Ticks
        };
    
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _grpcClient.GetBestValueAsync(request);
            var endTime = DateTime.UtcNow;
        
            if (response.Success)
            {
                var totalTime = endTime - startTime;
                var computationTime = TimeSpan.FromTicks(response.WorkerEndTicks - response.WorkerStartTicks);
                var communicationTime = totalTime - computationTime;
            
                // Log performance metrics
                GameMetricsLogger.LogTiming(
                    totalTime, 
                    computationTime, 
                    communicationTime,
                    depth, 
                    _granulationDepth, 
                    response.Success);
                
                return response.Value;
            }
            else
            {
                Console.WriteLine("[WARNING] Remote evaluation failed, falling back to local");
                return MinimaxSearch(board, depth, isMaximizing);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Remote evaluation failed: {ex.Message}, falling back to local");
            return MinimaxSearch(board, depth, isMaximizing);
        }
    }


}
