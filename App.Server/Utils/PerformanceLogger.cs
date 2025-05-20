using System;
using System.IO;
using System.Text;
using System.Threading;

public static class PerformanceLogger
{
    private static readonly string LogFilePath = "game_performance_log.txt";
    private static readonly object FileLock = new object();
    
    static PerformanceLogger()
    {
        // Create the log file header if it doesn't exist
        if (!File.Exists(LogFilePath))
        {
            using (StreamWriter writer = File.CreateText(LogFilePath))
            {
                writer.WriteLine("Timestamp,GameID,TotalTime(ms),CalculationTime(ms),CommunicationTime(ms),Depth,Granulation,Success");
            }
        }
    }
    
    public static void LogTiming(
        TimeSpan totalTime, 
        TimeSpan calculationTime, 
        TimeSpan communicationTime,
        int depth,
        int granulation,
        bool success,
        string gameId = null)
    {
        if (gameId == null)
        {
            gameId = Guid.NewGuid().ToString().Substring(0, 8);
        }
        
        var logEntry = new StringBuilder();
        logEntry.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},");
        logEntry.Append($"{gameId},");
        logEntry.Append($"{totalTime.TotalMilliseconds:F2},");
        logEntry.Append($"{calculationTime.TotalMilliseconds:F2},");
        logEntry.Append($"{communicationTime.TotalMilliseconds:F2},");
        logEntry.Append($"{depth},");
        logEntry.Append($"{granulation},");
        logEntry.Append($"{success}");
        
        lock (FileLock)
        {
            File.AppendAllText(LogFilePath, logEntry.ToString() + Environment.NewLine);
        }
    }
}