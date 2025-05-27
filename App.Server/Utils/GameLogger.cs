// Utilities/GameLogger.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class GameLogger
{
    private static readonly string GameLogFilePath = "game_log.csv";
    private static readonly string MinimaxPerformanceLogPath = "minimax_performance.csv";
    private static readonly string MinimaxSummaryLogPath = "minimax_summary.txt";
    private static readonly object LogLock = new object();
    
    // Statystyki minimaksa
    private static long _totalComputationTime = 0;
    private static long _totalCommunicationTime = 0;
    private static int _totalRequests = 0;
    private static Dictionary<int, (long totalTime, int count)> _depthStatistics = new Dictionary<int, (long, int)>();
    private static HashSet<string> _activeServers = new HashSet<string>();
    private static int _currentGranulation = 1; // Domyślna wartość
    private static DateTime _sessionStartTime = DateTime.Now;

    static GameLogger()
    {
        // Inicjalizacja pliku logów gry
        if (!File.Exists(GameLogFilePath))
        {
            File.WriteAllText(GameLogFilePath, "Depth;Granulation;TotalGameTimeSec\n");
        }
        
        // Inicjalizacja pliku logów wydajności minimaksa
        if (!File.Exists(MinimaxPerformanceLogPath))
        {
            File.WriteAllText(MinimaxPerformanceLogPath, 
                "Timestamp;Operation;Server;Depth;IsMaximizing;TotalTimeMs;ConversionTimeMs;NetworkTimeMs;ComputationTimeMs;Result\n");
        }
        
        // Sprawdź czy plik podsumowania istnieje, jeśli nie - utwórz go
        if (!File.Exists(MinimaxSummaryLogPath))
        {
            File.WriteAllText(MinimaxSummaryLogPath, "");
        }
    }

    public static void LogGame(int depth, int granulation, double gameTimeSeconds)
    {
        lock (LogLock)
        {
            // Zapisujemy granulację na potrzeby podsumowania
            _currentGranulation = granulation;
            
            var line = string.Format(CultureInfo.InvariantCulture,
                "{0};{1};{2:F2}\n",
                depth, granulation, gameTimeSeconds / 1000);

            File.AppendAllText(GameLogFilePath, line);
        }
    }
    
    public static void LogMinimaxOperation(
        string operation, 
        string server, 
        int depth, 
        bool isMaximizing, 
        long totalTimeMs, 
        long conversionTimeMs, 
        long networkTimeMs, 
        long computationTimeMs, 
        int result)
    {
        try
        {
            lock (LogLock)
            {
                // Dodajemy serwer do listy aktywnych serwerów (jeśli to nie jest "ALL" lub "NONE")
                if (server != "ALL" && server != "NONE" && !string.IsNullOrEmpty(server))
                {
                    _activeServers.Add(server);
                }
                
                // Aktualizacja statystyk
                _totalRequests++;
                _totalCommunicationTime += networkTimeMs;
                _totalComputationTime += computationTimeMs;
                
                if (!_depthStatistics.ContainsKey(depth))
                {
                    _depthStatistics[depth] = (totalTimeMs, 1);
                }
                else
                {
                    var (existingTotal, existingCount) = _depthStatistics[depth];
                    _depthStatistics[depth] = (existingTotal + totalTimeMs, existingCount + 1);
                }
                
                // Zapisz do pliku logów
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"{timestamp};{operation};{server};{depth};{isMaximizing};" +
                    $"{totalTimeMs};{conversionTimeMs};{networkTimeMs};{computationTimeMs};{result}\n";
                File.AppendAllText(MinimaxPerformanceLogPath, logEntry);
                
                // Usunięto automatyczne zapisywanie co 100 zapytań
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu do pliku logów: {ex.Message}");
        }
    }
    
    public static void WriteMinimaxSummary()
    {
        try
        {
            lock (LogLock)
            {
                // Jeśli nie ma żadnych zapytań, nie zapisuj podsumowania
                if (_totalRequests == 0)
                {
                    return;
                }
                
                var sb = new StringBuilder();
                sb.AppendLine("=== Minimax Performance Summary ===");
                sb.AppendLine($"Time: {DateTime.Now}");
                sb.AppendLine($"Session duration: {(DateTime.Now - _sessionStartTime).TotalMinutes:F2} minutes");
                sb.AppendLine($"Granulation: {_currentGranulation}");
                sb.AppendLine($"Active servers: {_activeServers.Count}");
                
                // Dodajemy listę adresów serwerów
                if (_activeServers.Count > 0)
                {
                    sb.AppendLine("Server addresses:");
                    foreach (var server in _activeServers.OrderBy(s => s))
                    {
                        sb.AppendLine($"  - {server}");
                    }
                }
                
                sb.AppendLine($"Total requests: {_totalRequests}");
                sb.AppendLine($"Average communication time: {(double)_totalCommunicationTime / _totalRequests:F2} ms");
                sb.AppendLine($"Average computation time: {(double)_totalComputationTime / _totalRequests:F2} ms");
                sb.AppendLine($"Total communication time: {_totalCommunicationTime} ms");
                sb.AppendLine($"Total computation time: {_totalComputationTime} ms");
                
                sb.AppendLine("\nBy depth:");
                foreach (var depthStat in _depthStatistics.OrderBy(d => d.Key))
                {
                    double avgTime = (double)depthStat.Value.totalTime / depthStat.Value.count;
                    sb.AppendLine($"  Depth {depthStat.Key}: {depthStat.Value.count} requests, avg time: {avgTime:F2} ms");
                }
                
                sb.AppendLine("\n=== End of Summary ===\n");
                
                // Dopisujemy na końcu pliku
                File.AppendAllText(MinimaxSummaryLogPath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu podsumowania: {ex.Message}");
        }
    }
    
    // Metoda do resetowania statystyk po zapisaniu podsumowania (opcjonalnie)
    public static void ResetStatistics()
    {
        lock (LogLock)
        {
            _totalComputationTime = 0;
            _totalCommunicationTime = 0;
            _totalRequests = 0;
            _depthStatistics.Clear();
            _activeServers.Clear();
            _sessionStartTime = DateTime.Now;
        }
    }
    
    // Metoda do rejestrowania listy serwerów (można wywołać przy inicjalizacji dystrybutora)
    public static void RegisterServers(IEnumerable<string> serverAddresses)
    {
        lock (LogLock)
        {
            foreach (var server in serverAddresses)
            {
                _activeServers.Add(server);
            }
        }
    }
    
    public static (long computationTime, long communicationTime, int requests) GetMinimaxStatistics()
    {
        lock (LogLock)
        {
            return (_totalComputationTime, _totalCommunicationTime, _totalRequests);
        }
    }
    
    public static Dictionary<int, (long totalTime, int count)> GetDepthStatistics()
    {
        lock (LogLock)
        {
            return new Dictionary<int, (long, int)>(_depthStatistics);
        }
    }
    
    public static int GetActiveServerCount()
    {
        lock (LogLock)
        {
            return _activeServers.Count;
        }
    }
    
    public static int GetCurrentGranulation()
    {
        lock (LogLock)
        {
            return _currentGranulation;
        }
    }
}