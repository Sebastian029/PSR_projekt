// CheckersAI.cs
using App.Client;
using App.Server;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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

        if (_serverAddresses.Count > 0)
        {
            _minimaxDistributor = new MinimaxDistributor(_serverAddresses);
            Console.WriteLine($"Initialized distributor with {_serverAddresses.Count} server(s)");
        }
        else
        {
            Console.WriteLine("NO SERVER AVAILABLE");
        }

        _minimax = new Minimax(depth, granulation, _evaluator, _minimaxDistributor);
    }

    // CheckersAI.cs
    public (int fromRow, int fromCol, int toRow, int toCol) CalculateOptimalMove(CheckersBoard board, bool isWhiteTurn)
    {
        try
        {
            var captures = _moveGenerator.GetMandatoryCaptures(board, isWhiteTurn);
            if (captures.Count > 0)
            {
                var (fromRow, fromCol, toRow, toCol) = _minimax.GetBestMove(board, captures, isWhiteTurn);
                return (fromRow, fromCol, toRow, toCol);
            }

            var validMoves = _moveGenerator.GetAllValidMoves(board, isWhiteTurn);
            if (validMoves.Count == 0)
            {
                Console.WriteLine("No valid moves available");
                return (-1, -1, -1, -1);
            }

            var (fromRow2, fromCol2, toRow2, toCol2) = _minimax.GetBestMove(board, validMoves, isWhiteTurn);
            return (fromRow2, fromCol2, toRow2, toCol2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CalculateOptimalMove: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return (-1, -1, -1, -1);
        }
    }


    public bool IsGameOver(CheckersBoard board) =>
        !_moveGenerator.HasValidMoves(board, true) || !_moveGenerator.HasValidMoves(board, false);

    public bool WhiteWon(CheckersBoard board) =>
        _moveGenerator.HasValidMoves(board, true) && !_moveGenerator.HasValidMoves(board, false);

    public void updateSettings(int depth, int granulation, List<string> serverAddresses = null)
    {
        _granulation = granulation;

        if (serverAddresses != null)
        {
            _serverAddresses = serverAddresses;
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

        _minimax = new Minimax(depth, granulation, _evaluator, _minimaxDistributor);
    }

    public void updateSettings(int depth, int granulation)
    {
        updateSettings(depth, granulation, null);
    }
}
