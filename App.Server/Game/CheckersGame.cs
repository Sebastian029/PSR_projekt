using Grpc.Net.Client;
using System.Collections.Generic;

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
    private List<string> _serverAddresses;

    public bool IsPlayerMode => _isPlayerMode;
    public bool IsWhiteTurn => isWhiteTurn;
    public int? MustCaptureFrom => mustCaptureFrom;

    // public CheckersGame()
    // {
    //     board = new CheckersBoard();
    //     isWhiteTurn = true;
    //     _serverAddresses = new List<string>();
    //     checkersAi = new CheckersAI(depth: 5, granulation: 1, isPerformanceTest: false, serverAddresses: _serverAddresses);
    // }
    public CheckersGame()
    {
        board = new CheckersBoard();
        isWhiteTurn = true;
        _serverAddresses = new List<string>();
        _serverAddresses.Add("http://localhost:5001");
        _serverAddresses.Add("http://localhost:5002");
        //_serverAddresses.Add("http://192.168.6.19:5001");
        //_serverAddresses.Add("http://192.168.6.19:5002");
        Console.WriteLine($"Using server address for distributed calculation: {_serverAddresses[0]}");
        checkersAi = new CheckersAI(depth: 5, granulation: 1, isPerformanceTest: false, serverAddresses: _serverAddresses);
    }

    public void SetDifficulty(int depth, int granulation, bool isPerformanceTest, bool isPlayerMode)
    {
        _depth = depth;
        _granulation = granulation;
        _isPerformanceTest = isPerformanceTest;
        _isPlayerMode = isPlayerMode;
        Console.WriteLine($"Game settings: Depth={depth}, Granulation={granulation}, PerfTest={isPerformanceTest}, PlayerMode={isPlayerMode}");
        checkersAi.updateSettings(depth, granulation);
    }

    public void SetServerAddresses(List<string> serverAddresses)
    {
        _serverAddresses = serverAddresses ?? new List<string>();
        Console.WriteLine($"Setting {_serverAddresses.Count} server addresses for distributed calculation");
        checkersAi.updateSettings(_depth, _granulation, _serverAddresses);
    }
    
    public void AddServerAddress(string address)
    {
        if (!string.IsNullOrEmpty(address) && !_serverAddresses.Contains(address))
        {
            _serverAddresses.Add(address);
            Console.WriteLine($"Added server address: {address}. Total servers: {_serverAddresses.Count}");
            checkersAi.updateSettings(_depth, _granulation, _serverAddresses);
        }
    }

    public int Depth
    {
        get { return _depth; }
    }

    public int Granulation
    {
        get { return _granulation; }
    }

    public bool IsPerformanceTest
    {
        get { return _isPerformanceTest; }
    }
    
    public List<string> ServerAddresses
    {
        get { return new List<string>(_serverAddresses); }
    }
}
