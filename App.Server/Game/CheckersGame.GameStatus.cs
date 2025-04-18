namespace App.Server;

public partial class CheckersGame
{
    public bool CheckGameOver()
    {
        return checkersAi.IsGameOver(board);
    }

    public bool HasWhiteWon()
    {
        return checkersAi.WhiteWon(board);
    }
}