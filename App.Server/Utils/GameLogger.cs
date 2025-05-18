// Utilities/GameLogger.cs
using System.Globalization;
using System.IO;

public static class GameLogger
{
    private static readonly string LogFilePath = "game_log.csv";

    static GameLogger()
    {
        if (!File.Exists(LogFilePath))
        {
            File.WriteAllText(LogFilePath, "Depth;Granulation;TotalGameTimeSec\n");
        }
    }

    public static void LogGame(int depth, int granulation,  double gameTimeSeconds)
    {
        var line = string.Format(CultureInfo.InvariantCulture,
            "{0}; {1}; {2:F2}\n",
            depth, granulation, gameTimeSeconds);

        File.AppendAllText(LogFilePath, line);
    }
}