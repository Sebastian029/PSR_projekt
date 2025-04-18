namespace App.Server;

public class MoveRequest
{
    public string type { get; set; } = "move";
    public int from { get; set; }
    public int to { get; set; }
}