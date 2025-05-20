using App.Server;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class MinimaxClient
{
    private readonly int _maxDepth;
    private readonly int _granulation;
    private readonly IBoardEvaluatorClient _evaluator;
    private readonly CheckersService.CheckersServiceClient _grpcClient;

    public MinimaxClient(int depth, int granulation, IBoardEvaluatorClient evaluator, GrpcChannel channel = null)
    {
        _maxDepth = depth;
        _granulation = granulation;
        _evaluator = evaluator;

        if (channel != null)
            _grpcClient = new CheckersService.CheckersServiceClient(channel);
    }

    public async Task<(int fromField, int toField)> GetBestMove(CheckersBoard board, bool isWhiteTurn)
    {
        if (_grpcClient != null)
        {
            return await DistributeBestMove(board, _maxDepth, isWhiteTurn);
        }

        return await GetBestMoveLocally(board, isWhiteTurn);
    }

    public async Task<int> MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        if (depth == 0 || new MoveGeneratorClient().IsGameOver(board))
            return _evaluator.EvaluateBoard(board, isMaximizing);

        var generator = new MoveGeneratorClient();
        var captures = generator.GetMandatoryCaptures(board, isMaximizing);
        var moves = captures.Count > 0
            ? generator.GetCaptureMoves(captures)
            : generator.GetAllValidMoves(board, isMaximizing);

        if (moves.Count == 0)
            return _evaluator.EvaluateBoard(board, isMaximizing);

        int bestEval = isMaximizing ? int.MinValue : int.MaxValue;

        // For shallow depths, process sequentially to avoid thread overhead
        if (depth <= 2)
        {
            foreach (var (from, to) in moves)
            {
                var simulated = board.Clone();
                if (captures.Count > 0)
                    new CaptureSimulatorClient().SimulateCapture(simulated, from, to);
                else
                    simulated.MovePiece(from, to);

                int eval = await MinimaxSearch(simulated, depth - 1, !isMaximizing);
                bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
            }
            
            return bestEval;
        }
        
        // For deeper searches, use parallel processing
        var tasks = new List<Task<int>>();

        foreach (var (from, to) in moves)
        {
            var simulated = board.Clone();
            if (captures.Count > 0)
                new CaptureSimulatorClient().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            tasks.Add(Task.Run(() => MinimaxSearch(simulated, depth - 1, !isMaximizing)));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var eval in results)
        {
            bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
        }

        return bestEval;
    }

    private async Task<(int fromField, int toField)> DistributeBestMove(CheckersBoard board, int depth, bool isWhiteTurn)
    {
        var request = new BoardStateRequest
        {
            BoardState = { board.board },
            IsWhiteTurn = isWhiteTurn,
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
                
                return (response.FromField, response.ToField);
            }
            else
            {
                return await GetBestMoveLocally(board, isWhiteTurn);
            }
        }
        catch (Exception)
        {
            return await GetBestMoveLocally(board, isWhiteTurn);
        }
    }

    private async Task<(int fromField, int toField)> GetBestMoveLocally(CheckersBoard board, bool isWhiteTurn)
    {
        var generator = new MoveGeneratorClient();
        var captures = generator.GetMandatoryCaptures(board, isWhiteTurn);
        var moves = captures.Count > 0
            ? generator.GetCaptureMoves(captures)
            : generator.GetAllValidMoves(board, isWhiteTurn);

        if (moves.Count == 0)
            return (-1, -1);
            
        if (moves.Count == 1)
            return moves[0];

        int bestValue = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int fromField, int toField) bestMove = (-1, -1);
        
        // Use parallel tasks to evaluate moves concurrently
        var tasks = new List<Task<(int from, int to, int value)>>();

        foreach (var (from, to) in moves)
        {
            // Capture the variables to avoid closure issues
            int fromField = from;
            int toField = to;
            
            tasks.Add(Task.Run(async () =>
            {
                var simulated = board.Clone();
                if (captures.Count > 0)
                    new CaptureSimulatorClient().SimulateCapture(simulated, fromField, toField);
                else
                    simulated.MovePiece(fromField, toField);

                int currentValue = await MinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn);
                return (fromField, toField, currentValue);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Find the best move from the parallel evaluations
        foreach (var result in results)
        {
            if ((isWhiteTurn && result.value > bestValue) || (!isWhiteTurn && result.value < bestValue))
            {
                bestValue = result.value;
                bestMove = (result.from, result.to);
            }
        }

        return bestMove;
    }
    
    // Backward compatibility method that wraps the async version
    public (int fromField, int toField) GetBestMoveSync(CheckersBoard board, bool isWhiteTurn)
    {
        return GetBestMove(board, isWhiteTurn).Result;
    }
}
