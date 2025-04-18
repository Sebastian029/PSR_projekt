namespace App.Server;

public partial class CheckersGame
{
    public bool CheckGameOver()
    {
        return aIntelligence.IsGameOver(board);
    }

    public bool HasWhiteWon()
    {
        return aIntelligence.WhiteWon(board);
    }
}