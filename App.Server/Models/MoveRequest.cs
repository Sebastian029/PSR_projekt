namespace App.Server;

public class MoveRequest
{
    public MoveRequest(int from, int to)
    {
        this.from = from;
        this.to = to;
    }
    
    public string type { get; set; } = "move";
    public int from { get; set; }
    public int to { get; set; }
    
    
}