using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;

public static class GameMetricsLogger
{
    private static readonly string LogFilePath = "game_metrics.csv";
    private static readonly object FileLock = new object();
    private static double _totalCalculationTime = 0;
    private static double _totalCommunicationTime = 0;
    private static int _operationsCount = 0;
    
    static GameMetricsLogger()
    {
        // Create the log file with headers if it doesn't exist
        if (!File.Exists(LogFilePath))
        {
            using (StreamWriter writer = File.CreateText(LogFilePath))
            {
                writer.WriteLine("Depth;Granulation;Workers;TotalGameTime;TotalGameCalculationTime;TotalGameCommunicationTime");
            }
        }
    }
    
    public static void ResetMetrics()
    {
        _totalCalculationTime = 0;
        _totalCommunicationTime = 0;
        _operationsCount = 0;
    }
    
    public static void LogTiming(
        TimeSpan totalTime, 
        TimeSpan calculationTime, 
        TimeSpan communicationTime,
        int depth,
        int granulation,
        bool success)
    {
        if (success)
        {
            // Store calculation time in seconds instead of milliseconds
            _totalCalculationTime += calculationTime.TotalSeconds;
            // Store communication time in seconds instead of milliseconds
            _totalCommunicationTime += communicationTime.TotalSeconds;
            _operationsCount++;
        }
    }
    
    public static void LogGame(
        int depth,
        int granulation,
        int workersCount,
        double totalGameTimeSeconds)
    {
        var logEntry = new StringBuilder();
        logEntry.Append($"{depth};");
        logEntry.Append($"{granulation};");
        logEntry.Append($"{workersCount};");
        logEntry.Append($"{totalGameTimeSeconds:F2};");
        // Format calculation time in seconds with 2 decimal places
        logEntry.Append($"{_totalCalculationTime:F2};");
        // Format communication time in seconds with 2 decimal places
        logEntry.Append($"{_totalCommunicationTime:F2}");
        
        lock (FileLock)
        {
            File.AppendAllText(LogFilePath, logEntry.ToString() + Environment.NewLine);
        }
        
        // Reset metrics for the next game
        ResetMetrics();
    }
}
