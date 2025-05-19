using App.Server;
using Grpc.Core;
using GrpcServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly int _granulationDepth;
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
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        int bestFrom = -1;
        int bestTo = -1;

        foreach (var (from, to) in moves)
        {
            var simulated = board.Clone();
            simulated.MovePiece(from, to);
            
            // Calculate score for this move
            int score = EvaluatePosition(simulated, _maxDepth - 1, !isWhiteTurn);
            
            if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
            {
                bestScore = score;
                bestFrom = from;
                bestTo = to;
            }
        }

        return (bestFrom, bestTo);
    }

    public (int fromField, int toField) GetBestCapture(CheckersBoard board, Dictionary<int, List<int>> captures, bool isWhiteTurn)
    {
        var moves = captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key, to))).ToList();
        int bestScore = isWhiteTurn ? int.MinValue : int.MaxValue;
        int bestFrom = -1;
        int bestTo = -1;

        foreach (var (from, to) in moves)
        {
            var simulated = board.Clone();
            new CaptureSimulator().SimulateCapture(simulated, from, to);
            
            // Calculate score for this capture
            int score = EvaluatePosition(simulated, _maxDepth - 1, !isWhiteTurn);
            
            if ((isWhiteTurn && score > bestScore) || (!isWhiteTurn && score < bestScore))
            {
                bestScore = score;
                bestFrom = from;
                bestTo = to;
            }
        }

        return (bestFrom, bestTo);
    }

    private int EvaluatePosition(CheckersBoard board, int depth, bool isMaximizing)
    {
        if (depth == 0 || new MoveGenerator().IsGameOver(board))
            return _evaluator.EvaluateBoard(board, isMaximizing);

        // If we've reached the granulation depth and have a gRPC client, distribute the calculation
        if (_grpcClient != null && _maxDepth - depth >= _granulationDepth)
        {
            return DistributeScoreCalculation(board, depth, isMaximizing).Result;
        }

        // Otherwise, continue with local calculation
        return MinimaxSearch(board, depth, isMaximizing);
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

        if (moves.Count == 0)
            return _evaluator.EvaluateBoard(board, isMaximizing);

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

    private async Task<int> DistributeScoreCalculation(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine($"[REMOTE] Distributing score calculation: Depth={depth}");
    
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
            var response = await _grpcClient.EvaluatePositionAsync(request);
            var endTime = DateTime.UtcNow;

            if (response.Success)
            {
                var totalTime = endTime - startTime;
                var computationTime = TimeSpan.FromTicks(response.WorkerEndTicks - response.WorkerStartTicks);
                var communicationTime = totalTime - computationTime;
            
                // Log performance metrics
                PerformanceLogger.LogTiming(totalTime, computationTime, communicationTime,
                    depth, _granulationDepth, response.Success);
            
                return response.Score;
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
