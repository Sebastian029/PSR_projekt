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

    public CheckersAI(int depth = 5, int granulation = 1, bool? isPerformanceTest = false)
    {
        _evaluator = new Evaluator();
        _moveGenerator = new MoveGenerator();
        _captureSimulator = new CaptureSimulator();
        _minimax = new Minimax(depth, _evaluator);
    }
}
