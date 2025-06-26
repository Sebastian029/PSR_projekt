namespace App.Server;

public class GameStateResponse
{
    public bool Success { get; set; }
    public string Board { get; set; }
    public bool IsWhiteTurn { get; set; }
    public bool GameOver { get; set; }
    public string Winner { get; set; }
    public string CurrentPlayer { get; set; } 
}