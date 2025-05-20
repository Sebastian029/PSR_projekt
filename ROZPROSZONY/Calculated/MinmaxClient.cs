using App.Server;
using Grpc.Core;
using GrpcServer;

public class MinimaxClient
{
    private readonly int _maxDepth;
    private readonly int _granulation;
    private readonly IBoardEvaluatorClient _evaluator;

    public MinimaxClient(int depth, int granulation, IBoardEvaluatorClient evaluator)
    {
        _maxDepth = depth;
        _granulation = granulation;
        _evaluator = evaluator;
        Console.WriteLine("GET DEPTH " +  depth);
        
    }

   
    public int MinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
    {
        if(depth >= 3)
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
                new CaptureSimulatorClient().SimulateCapture(simulated, from, to);
            else
                simulated.MovePiece(from, to);

            int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
            bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
        }

        return bestEval;
    }


}
