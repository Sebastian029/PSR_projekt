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

        foreach (var move in moves)
        {
            var simulated = board.Clone();
            simulated.MovePiece(move.from, move.to);

            int score;
            if (_grpcClient != null && _maxDepth > _granulation)
            {
                var result = GranulatedMinimaxWithMoveParallel(simulated, _maxDepth - 1, !isWhiteTurn, _granulation).Result;
                score = result.value;
            }
            else
            {
                score = MinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn);
            }

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

    public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine($"[REMOTE] MinimaxSearch: Depth={depth}");

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

    private async Task<(int value, int from, int to)> GranulatedMinimaxWithMoveParallel(CheckersBoard board, int depth, bool isMaximizing, int granulation)
    {
        var generator = new MoveGenerator();
        var captures = generator.GetMandatoryCaptures(board, isMaximizing);
        var moves = captures.Count > 0
            ? generator.GetCaptureMoves(captures)
            : generator.GetAllValidMoves(board, isMaximizing);

        // Tworzymy listę zadań z poprawnym typem zwracanym
        var tasks = moves.Select(async move =>
        {
            var (from, to) = move;
            var simulated = board.Clone();

            if (captures.Count > 0)
                new CaptureSimulator().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            int score = await DistributedMinimaxSearch(simulated, depth - 1, !isMaximizing);
            return (value: score, from, to); // Zwracamy w odpowiednim formacie
        }).ToList();

        var results = await Task.WhenAll(tasks);

        if (isMaximizing)
        {
            var best = results.OrderByDescending(r => r.value).First();
            return best;
        }
        else
        {
            var best = results.OrderBy(r => r.value).First();
            return best;
        }
    }


    private (int score, int from, int to) GetBestMoveLocal(CheckersBoard board, int depth, bool isMaximizing)
    {
        var generator = new MoveGenerator();
        var captures = generator.GetMandatoryCaptures(board, isMaximizing);
        var moves = captures.Count > 0
            ? generator.GetCaptureMoves(captures)
            : generator.GetAllValidMoves(board, isMaximizing);

        int bestEval = isMaximizing ? int.MinValue : int.MaxValue;
        (int from, int to) bestMove = (-1, -1);

        foreach (var (from, to) in moves)
        {
            var simulated = board.Clone();

            if (captures.Count > 0)
                new CaptureSimulator().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);

            if ((isMaximizing && eval > bestEval) || (!isMaximizing && eval < bestEval))
            {
                bestEval = eval;
                bestMove = (from, to);
            }
        }

        return (bestEval, bestMove.from, bestMove.to);
    }


    private async Task<int> DistributedMinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine($"[LOCAL] DistributedMinimaxSearch: Depth={depth}");

        var request = new BoardStateRequest
        {
            BoardState = { board.board },
            IsWhiteTurn = isMaximizing,
            Depth = depth,
            Granulation = 0
        };

        try
        {
            var response = await _grpcClient.GetBestValueAsync(request);
            if (response.Success)
            {
                Console.WriteLine(response.Value);
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
