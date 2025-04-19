using Grpc.Net.Client;
using GrpcServer;

namespace App.Server;

public partial class CheckersGame
{
    private CheckersBoard board;
    private CheckersAI checkersAi;
    private bool isWhiteTurn;
    private int? mustCaptureFrom = null;
    private List<int> captureSequence = new();
    private int _depth;
    private int _granulation;
    private bool _isPerformanceTest;
    private bool _isPlayerMode;
    private CheckersService.CheckersServiceClient _client;

    public bool IsPlayerMode => _isPlayerMode;
    public bool IsWhiteTurn => isWhiteTurn;
    public int? MustCaptureFrom => mustCaptureFrom;

    public CheckersGame()
    {
        board = new CheckersBoard();
        isWhiteTurn = true;
        var channel = GrpcChannel.ForAddress("http://localhost:5000");
        _client = new CheckersService.CheckersServiceClient(channel);
        checkersAi = new CheckersAI(_depth= 5, granulation: 1, isPerformanceTest: false, grpcChannel: channel);
    }

    public void SetDifficulty(int depth, int granulation, bool isPerformanceTest, bool isPlayerMode)
    {
        _depth = depth;
        _granulation = granulation;
        _isPerformanceTest = isPerformanceTest;
        _isPlayerMode = isPlayerMode;
        Console.WriteLine($"Game settings: Depth={depth}, Granulation={granulation}, PerfTest={isPerformanceTest}, PlayerMode={isPlayerMode}");
    }
}