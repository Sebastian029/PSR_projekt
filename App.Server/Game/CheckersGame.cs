// CheckersGame.cs
using Grpc.Net.Client;
using System.Collections.Generic;

namespace App.Server;

public partial class CheckersGame
{
    private CheckersBoard board;
    private CheckersAI checkersAi;
    private bool isWhiteTurn;
    private (int row, int col)? mustCaptureFrom = null;
    private List<(int row, int col)> captureSequence = new();
    private int _depth;
    private int _granulation;
    private bool _isPerformanceTest;
    private bool _isPlayerMode;
    private List<string> _serverAddresses;
    
    private List<string> boardHistory = new List<string>();
    private Dictionary<string, int> positionCount = new Dictionary<string, int>();
    private const int MAX_POSITION_REPEATS = 3;
    private const int MAX_MOVES_WITHOUT_CAPTURE = 50;
    private int movesWithoutCapture = 0;
    // NOWE POLA dla stanu gry
    private bool gameOver = false;
    private string winner = null;
    private string drawReason = null;

    // NOWE WŁAŚCIWOŚCI
    public bool IsGameOver => gameOver;
    public string Winner => winner;
    public string DrawReason => drawReason;
    public bool IsDrawGame => winner == "draw";

    public bool IsPlayerMode => _isPlayerMode;
    public bool IsWhiteTurn => isWhiteTurn;
    public (int row, int col)? MustCaptureFrom => mustCaptureFrom;

    // public CheckersGame()
    // {
    //     board = new CheckersBoard();
    //     isWhiteTurn = true;
    //     _serverAddresses = new List<string>();
    //     _serverAddresses.Add("http://localhost:5001");
    //     _serverAddresses.Add("http://localhost:5002");
    //     
    //     Console.WriteLine($"Using server address for distributed calculation: {_serverAddresses[0]}");
    //     checkersAi = new CheckersAI(depth: 5, granulation: 1, isPerformanceTest: false, serverAddresses: _serverAddresses);
    // }
    // CheckersGame.cs - konstruktor
    public CheckersGame()
    {
        board = new CheckersBoard();
        isWhiteTurn = true;
        _serverAddresses = new List<string>();
    
        // Wyłącz serwery dla testów lokalnych
        _serverAddresses.Add("http://192.168.17.240:5001");
        _serverAddresses.Add("http://192.168.17.19:5001");
        _serverAddresses.Add("http://localhost:5001");
    
        Console.WriteLine("Using LOCAL calculation only (no distributed servers)");
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
    // CheckersGame.cs - dodaj tę metodę jeśli jej nie ma
    public void SwitchTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        mustCaptureFrom = null;
        captureSequence.Clear();
        Console.WriteLine($"Turn forcibly switched to {(isWhiteTurn ? "White" : "Black")}");
    }
    
    public void CheckForStalemate()
{
    var validMoves = GetValidMovesFiltered();
    if (validMoves.Count == 0)
    {
        Console.WriteLine("No valid moves available - stalemate");
        EndGameWithDraw("Stalemate - no valid moves available");
    }
}
    public bool CheckForDraw()
    {
        return IsDrawByRepetition() || IsDrawBy50MoveRule();
    }

    

public bool CheckGameOver()
{
    if (gameOver) return true;

    // Sprawdź remis przez 50 ruchów
    if (movesWithoutCapture >= MAX_MOVES_WITHOUT_CAPTURE)
    {
        EndGameWithDraw("50 moves without capture");
        return true;
    }

    // Sprawdź remis przez powtórzenie pozycji
    if (IsDrawByRepetition())
    {
        EndGameWithDraw("Position repeated too many times");
        return true;
    }

    // Sprawdź czy któryś gracz nie ma figur
    bool hasWhitePieces = false;
    bool hasBlackPieces = false;
    
    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            if (board.IsDarkSquare(row, col))
            {
                PieceType piece = board.GetPiece(row, col);
                if (piece == PieceType.WhitePawn || piece == PieceType.WhiteKing)
                    hasWhitePieces = true;
                if (piece == PieceType.BlackPawn || piece == PieceType.BlackKing)
                    hasBlackPieces = true;
            }
        }
    }

    if (!hasWhitePieces)
    {
        EndGameWithWinner("black", "White has no pieces left");
        return true;
    }
    
    if (!hasBlackPieces)
    {
        EndGameWithWinner("white", "Black has no pieces left");
        return true;
    }

    // Sprawdź pat (brak możliwych ruchów)
    var validMoves = GetValidMovesFiltered();
    if (validMoves.Count == 0)
    {
        EndGameWithDraw("Stalemate - no valid moves available");
        return true;
    }

    return false;
}

private void EndGameWithWinner(string winnerColor, string reason)
{
    Console.WriteLine($"Game ended. Winner: {winnerColor}. Reason: {reason}");
    gameOver = true;
    winner = winnerColor;
    drawReason = null;
    
    GameLogger.WriteMinimaxSummary();

}

private void EndGameWithDraw(string reason)
{
    Console.WriteLine($"Game ended in draw: {reason}");
    gameOver = true;
    winner = "draw";
    drawReason = reason;
    
    GameLogger.WriteMinimaxSummary();

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

    public void RemoveServerAddress(string address)
    {
        if (_serverAddresses.Contains(address))
        {
            _serverAddresses.Remove(address);
            Console.WriteLine($"Removed server address: {address}. Total servers: {_serverAddresses.Count}");
            checkersAi.updateSettings(_depth, _granulation, _serverAddresses);
        }
    }

    public int Depth => _depth;
    public int Granulation => _granulation;
    public bool IsPerformanceTest => _isPerformanceTest;
    public List<string> ServerAddresses => new List<string>(_serverAddresses);
}
