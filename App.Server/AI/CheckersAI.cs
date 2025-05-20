using App.Server;
using Grpc.Core;

public class CheckersAI
{
    private  Minimax _minimax;
    private readonly MoveGenerator _moveGenerator;
    private readonly CaptureSimulator _captureSimulator;
    private readonly ChannelBase _channel;
    private readonly Evaluator evaluator;

    public CheckersAI(int depth = 5, int granulation = 1, bool? isPerformanceTest = false, ChannelBase grpcChannel = null)
    {
        evaluator = new Evaluator();
        _channel = grpcChannel;
        //_minimax = new Minimax(depth, granulation, evaluator, grpcChannel);
        _minimax = new Minimax(depth, evaluator);
        _moveGenerator = new MoveGenerator();
        _captureSimulator = new CaptureSimulator();
    }
    
    public (int fromField, int toField) CalculateOptimalMove(CheckersBoard board, bool isWhiteTurn)
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

    public void updateSettings(int depth, int granulation, bool performanceTest)
    {
        //_minimax = new Minimax(depth, granulation, evaluator, _channel);
        _minimax = new Minimax(depth, evaluator);


    }
}