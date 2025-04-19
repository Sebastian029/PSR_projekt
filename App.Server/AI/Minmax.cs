using App.Server;
using Grpc.Core;
using GrpcServer;
using System.Threading.Tasks;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly IBoardEvaluator _evaluator;
    private readonly CheckersService.CheckersServiceClient _grpcClient;

    public Minimax(int depth, IBoardEvaluator evaluator, ChannelBase grpcChannel = null)
    {
        _maxDepth = depth;
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
            Console.WriteLine("Max Depth:" + _maxDepth);

            if (_maxDepth > 1 && _grpcClient != null)
            {
                // Distribute the calculation for subtrees
                score = DistributedMinimaxSearch(simulated, _maxDepth - 1, !isWhiteTurn).Result;
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

    private int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        Console.WriteLine("Depth:" + depth);

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
            Granulation = (_evaluator as Evaluator)?._granulation ?? 1
        };

        try
        {
            var response = await _grpcClient.GetBestMoveAsync(request);
            if (response.Success)
            {
                var tempBoard = board.Clone();
                tempBoard.MovePiece(response.FromField, response.ToField);
                return _evaluator.EvaluateBoard(tempBoard, isMaximizing);
            }
            else
            {
               // return MinimaxSearch(board, depth, isMaximizing);
               return -1;
            }
        }
        catch
        {
            //return MinimaxSearch(board, depth, isMaximizing);
            return -1;
        }
    }
}