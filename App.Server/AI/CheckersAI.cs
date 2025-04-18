using App.Server;


public class CheckersAI
{
    private readonly Minimax _minimax;
    private readonly MoveGenerator _moveGenerator;
    private readonly CaptureSimulator _captureSimulator;

    public CheckersAI(int depth = 5, int granulation = 1, bool isPerformanceTest = false)
    {
        var evaluator = new Evaluator(granulation);
        _minimax = new Minimax(depth, evaluator);
        _moveGenerator = new MoveGenerator();
        _captureSimulator = new CaptureSimulator();
    }

    public (int fromField, int toField) GetBestMove(CheckersBoard board, bool isWhiteTurn)
    {
        var captures = _moveGenerator.GetMandatoryCaptures(board, isWhiteTurn);
        if (captures.Count > 0)
        {
            return _minimax.GetBestCapture(board, captures, isWhiteTurn);
        }

        var validMoves = _moveGenerator.GetAllValidMoves(board, isWhiteTurn);
        return _minimax.GetBestMove(board, validMoves, isWhiteTurn);
    }

    public bool IsGameOver(CheckersBoard board) =>
        !_moveGenerator.HasValidMoves(board, true) || !_moveGenerator.HasValidMoves(board, false);

    public bool WhiteWon(CheckersBoard board) =>
        _moveGenerator.HasValidMoves(board, true) && !_moveGenerator.HasValidMoves(board, false);
}