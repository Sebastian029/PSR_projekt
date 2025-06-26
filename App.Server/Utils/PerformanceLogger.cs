using System;
using System.IO;
using System.Text;
using System.Threading;

public static class PerformanceLogger
{
    private static readonly string LogFilePath = "performance_log.txt";
    private static readonly object LockObject = new object();
    private static bool _isFirstWrite = true;

    static PerformanceLogger()
    {
        lock (LockObject)
        {
            if (!File.Exists(LogFilePath))
            {
                WriteHeader();
            }
        }
    }

    private static void WriteHeader()
    {
        var header = new StringBuilder();

        header.AppendLine("# Performance metrics log");
        header.AppendLine("# Columns description:");
        header.AppendLine("# 1. Timestamp - date and time of measurement (UTC)");
        header.AppendLine("# 2. TotalTimeMs - total request time in milliseconds (client perspective)");
        header.AppendLine("# 3. ComputationTimeMs - time spent on actual calculations in milliseconds");
        header.AppendLine("# 4. CommunicationTimeMs - network communication time in milliseconds");
        header.AppendLine("# 5. Depth - search depth used in minimax algorithm");
        header.AppendLine("# 6. Granulation - granulation level used in distributed computing");
        header.AppendLine("# 7. Success - whether the operation succeeded (True/False)");
        header.AppendLine("#");
        header.AppendLine("# Data rows:");
        header.AppendLine("Timestamp,TotalTimeMs,ComputationTimeMs,CommunicationTimeMs,Depth,Granulation,Success");

        File.WriteAllText(LogFilePath, header.ToString());
    }

    public static void LogTiming(TimeSpan totalTime, TimeSpan computationTime, TimeSpan communicationTime,
                               int depth, int granulation, bool success)
    {
        lock (LockObject)
        {
            if (_isFirstWrite && File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length == 0)
            {
                WriteHeader();
                _isFirstWrite = false;
            }

            var logMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}," +
                           $"{totalTime.TotalMilliseconds:F2}," +
                           $"{computationTime.TotalMilliseconds:F2}," +
                           $"{communicationTime.TotalMilliseconds:F2}," +
                           $"{depth}," +
                           $"{granulation}," +
                           $"{(success ? "True" : "False")}";

            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
    }
}