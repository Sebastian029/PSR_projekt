using System.Globalization;
using System.IO;

public static class GameLogger
{
    private static readonly string LogFilePath = "game_log.txt";
    
    static GameLogger()
    {
        if (!File.Exists(LogFilePath))
        {
            File.WriteAllText(LogFilePath, "Depth; Granulation; Workers; TotalGameTimeSec;\n");
        }
    }

    public static void LogGame(int depth, int granulation, int workersCount, double gameTimeSeconds)
    {
        var line = string.Format(CultureInfo.InvariantCulture,
            "{0}; {1}; {2}; {3:F2}\n",
            depth, granulation, workersCount, gameTimeSeconds);
        File.AppendAllText(LogFilePath, line);
    }
}