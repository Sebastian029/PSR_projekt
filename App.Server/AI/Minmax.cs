using App.Server;
using Grpc.Core;
using GrpcServer;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly int _granulation;
    private readonly IBoardEvaluator _evaluator;
    private readonly CheckersService.CheckersServiceClient _grpcClient;

    public Minimax(int depth, int granulation, IBoardEvaluator evaluator, ChannelBase grpcChannel = null)
    {
        _maxDepth = depth;
        _granulation = granulation;
        _evaluator = evaluator;
        _grpcClient = grpcChannel != null ? new CheckersService.CheckersServiceClient(grpcChannel) : null;
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, List<(int from, int to)> moves, bool isWhiteTurn)
    {
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int from, int to) bestMove = (-1, -1);

        // First few depths are handled locally (granulation)
        if (_maxDepth <= _granulation)
        {
            return GetBestMoveLocal(board, moves, isWhiteTurn);
        }

        // Distribute remaining depth calculations to clients
        var tasks = moves.Select(async move =>
        {
            var simulated = board.Clone();
            simulated.MovePiece(move.from, move.to);
            
            int score = await DistributedMinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn);
            return (score: score, move: move);
        }).ToList();

        Task.WaitAll(tasks.ToArray());

        foreach (var task in tasks)
        {
            var (score, move) = task.Result;
            if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    public (int fromField, int toField) GetBestCapture(CheckersBoard board, Dictionary<int, List<int>> captures, bool isWhiteTurn)
    {
        var moves = captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key, to))).ToList();
        return GetBestMove(board, moves, isWhiteTurn);
    }

    private (int from, int to) GetBestMoveLocal(CheckersBoard board, List<(int from, int to)> moves, bool isWhiteTurn)
    {
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int from, int to) bestMove = (-1, -1);

        foreach (var move in moves)
        {
            var simulated = board.Clone();
            simulated.MovePiece(move.from, move.to);

            int score = MinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn);

            if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
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

    private async Task<int> DistributedMinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        var request = new BoardStateRequest
        {
            BoardState = { board.board },
            IsWhiteTurn = isMaximizing,
            Depth = depth,
            Granulation = _granulation,
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

                Console.WriteLine($"[TIMING] Total: {totalTime.TotalMilliseconds}ms, " +
                                $"Computation: {computationTime.TotalMilliseconds}ms, " +
                                $"Communication: {communicationTime.TotalMilliseconds}ms");

                PerformanceLogger.LogTiming(totalTime, computationTime, communicationTime,
                                          depth, _granulation, response.Success);

                return response.Value;
            }
            else
            {
                PerformanceLogger.LogTiming(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero,
                                          depth, _granulation, false);
                Console.WriteLine("[WARNING] Remote evaluation failed, falling back to local");
                return MinimaxSearch(board, depth, isMaximizing);
            }
        }
        catch (Exception ex)
        {
            PerformanceLogger.LogTiming(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero,
                                      depth, _granulation, false);
            Console.WriteLine($"[ERROR] Remote evaluation failed: {ex.Message}, falling back to local");
            return MinimaxSearch(board, depth, isMaximizing);
        }
    }
}