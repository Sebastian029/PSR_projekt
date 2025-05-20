using System;
using System.IO;
using System.Text;
using System.Threading;

public static class GameLogger
{
    private static readonly string LogFilePath = "completed_games_log.txt";
    private static readonly object FileLock = new object();
    
    static GameLogger()
    {
        // Create the log file header if it doesn't exist
        if (!File.Exists(LogFilePath))
        {
            using (StreamWriter writer = File.CreateText(LogFilePath))
            {
                writer.WriteLine("Timestamp,Depth,Granulation,WorkersCount,TotalGameTime(s),Result");
            }
        }
    }
    
    public static void LogGame(
        int depth,
        int granulation,
        int workersCount,
        double totalGameTimeSeconds,
        string result = "Completed")
    {
        var logEntry = new StringBuilder();
        logEntry.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},");
        logEntry.Append($"{depth},");
        logEntry.Append($"{granulation},");
        logEntry.Append($"{workersCount},");
        logEntry.Append($"{totalGameTimeSeconds:F2},");
        logEntry.Append($"{result}");
        
        lock (FileLock)
        {
            File.AppendAllText(LogFilePath, logEntry.ToString() + Environment.NewLine);
        }
    }
}