using App.Server;

public class Minimax
{
    private readonly int _maxDepth;
    private readonly IBoardEvaluator _evaluator;

    public Minimax(int depth, IBoardEvaluator evaluator)
    {
        _maxDepth = depth;
        _evaluator = evaluator;
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, List<(int from, int to)> moves, bool isWhiteTurn)
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

    public (int fromField, int toField) GetBestCapture(CheckersBoard board, Dictionary<int, List<int>> captures, bool isWhiteTurn)
    {
        var moves = captures.SelectMany(kvp => kvp.Value.Select(to => (kvp.Key, to))).ToList();
        return GetBestMove(board, moves, isWhiteTurn);
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
                new CaptureSimulator().SimulateCapture(simulated, from, to); // Use capitalized property names
            else
                simulated.MovePiece(from, to); // Use capitalized property names

            int eval = MinimaxSearch(simulated, depth - 1, !isMaximizing);
            bestEval = isMaximizing ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);
        }

        return bestEval;
    }

}
