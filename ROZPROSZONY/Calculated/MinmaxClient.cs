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
            
        Console.WriteLine("GET DEPTH " + depth);
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, bool isWhiteTurn)
    {
        if (_grpcClient != null)
        {
            return DistributeBestMove(board, _maxDepth, isWhiteTurn).Result;
        }

        return GetBestMoveLocally(board, isWhiteTurn);
    }

    public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine($"[LOCAL] MinimaxSearch: Depth={depth}");
        
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

        foreach (var (from, to) in moves)
        {
            var simulated = board.Clone();
            if (captures.Count > 0)
                new CaptureSimulatorClient().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
            bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
        }

        return bestEval;
    }

    private async Task<(int fromField, int toField)> DistributeBestMove(CheckersBoard board, int depth, bool isWhiteTurn)
    {
        Console.WriteLine($"[REMOTE] Distributing best move calculation: Depth={depth}");
        
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
                
                Console.WriteLine($"[PERF] Total: {totalTime.TotalMilliseconds:F1}ms, " +
                                  $"Computation: {computationTime.TotalMilliseconds:F1}ms, " +
                                  $"Communication: {communicationTime.TotalMilliseconds:F1}ms");
                
                return (response.FromField, response.ToField);
            }
            else
            {
                Console.WriteLine("[WARNING] Remote calculation failed, falling back to local");
                return GetBestMoveLocally(board, isWhiteTurn);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Remote calculation failed: {ex.Message}, falling back to local");
            return GetBestMoveLocally(board, isWhiteTurn);
        }
    }

    private (int fromField, int toField) GetBestMoveLocally(CheckersBoard board, bool isWhiteTurn)
    {
        var generator = new MoveGeneratorClient();
        var captures = generator.GetMandatoryCaptures(board, isWhiteTurn);
        var moves = captures.Count > 0
            ? generator.GetCaptureMoves(captures)
            : generator.GetAllValidMoves(board, isWhiteTurn);

        int bestValue = isWhiteTurn ? int.MinValue : int.MaxValue;
        (int fromField, int toField) bestMove = (-1, -1);

        foreach (var (from, to) in moves)
        {
            var simulated = board.Clone();
            if (captures.Count > 0)
                new CaptureSimulatorClient().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            int currentValue = MinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn);
            if ((isWhiteTurn && currentValue > bestValue) ||
                (!isWhiteTurn && currentValue < bestValue))
            {
                bestValue = currentValue;
                bestMove = (from, to);
            }
        }

        return bestMove;
    }
}
