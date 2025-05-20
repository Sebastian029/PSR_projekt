using App.Client;
using App.Server;
using Grpc.Core;
using System;
using System.Collections.Generic;

public class CheckersAI
{
    private Minimax _minimax;
    private MinimaxDistributor _minimaxDistributor;
    private readonly MoveGenerator _moveGenerator;
    private readonly CaptureSimulator _captureSimulator;
    private readonly Evaluator _evaluator;
    private List<string> _serverAddresses;
    private int _granulation;

    public CheckersAI(int depth = 5, int granulation = 1, bool? isPerformanceTest = false, List<string> serverAddresses = null)
    {
        _evaluator = new Evaluator();
        _moveGenerator = new MoveGenerator();
        _captureSimulator = new CaptureSimulator();
        _serverAddresses = serverAddresses ?? new List<string>();
        _granulation = granulation;
        
        // Initialize the distributor if server addresses are provided
        if (_serverAddresses.Count > 0)
        {
            _minimaxDistributor = new MinimaxDistributor(_serverAddresses);
            Console.WriteLine($"Initialized distributor with {_serverAddresses.Count} server(s)");
        }
        else
        {
            Console.WriteLine("NO SERVER AVAILABLE");
        }

        // Create Minimax instance with the correct parameter order
        _minimax = new Minimax(depth,granulation, _evaluator, _minimaxDistributor);
    }
    
    public (int fromField, int toField) CalculateOptimalMove(CheckersBoard board, bool isWhiteTurn)
    {
        var captures = _moveGenerator.GetMandatoryCaptures(board, isWhiteTurn);
        if (captures.Count > 0)
        {
            return _minimax.GetBestMove(board, captures, isWhiteTurn);
        }

        var validMoves = _moveGenerator.GetAllValidMoves(board, isWhiteTurn);
        return _minimax.GetBestMove(board, validMoves, isWhiteTurn);
    }

    public bool IsGameOver(CheckersBoard board) =>
        !_moveGenerator.HasValidMoves(board, true) || !_moveGenerator.HasValidMoves(board, false);

    public bool WhiteWon(CheckersBoard board) =>
        _moveGenerator.HasValidMoves(board, true) && !_moveGenerator.HasValidMoves(board, false);
    
    // Update method with only depth, granulation, and addresses
    public void updateSettings(int depth, int granulation, List<string> serverAddresses = null)
    {
        _granulation = granulation;
        
        // Update server addresses if provided
        if (serverAddresses != null)
        {
            _serverAddresses = serverAddresses;
            
            // Recreate the distributor if there are server addresses
            if (_serverAddresses.Count > 0)
            {
                _minimaxDistributor = new MinimaxDistributor(_serverAddresses);
                Console.WriteLine($"Updated distributor with {_serverAddresses.Count} server(s)");
            }
            else
            {
                _minimaxDistributor = null;
                Console.WriteLine("NO SERVER AVAILABLE");
            }
        }
        
        _minimax = new Minimax(depth,granulation, _evaluator, _minimaxDistributor);
    }
    
    public void updateSettings(int depth, int granulation)
    {
        updateSettings(depth, granulation, null);
    }
}
