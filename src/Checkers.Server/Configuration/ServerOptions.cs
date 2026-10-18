namespace Checkers.Server.Configuration;

public sealed class ServerOptions
{
    public const string SectionName = "Checkers";

    public List<string> Workers { get; set; } = [];

    public int ConcurrencyPerWorker { get; set; } = 4;

    public int WorkerTimeoutSeconds { get; set; } = 120;

    public int WorkerCooldownSeconds { get; set; } = 15;

    public int AiMoveDelayMs { get; set; } = 600;

    public string? MetricsCsvPath { get; set; } = "logs/search_metrics.csv";

    public List<string> AllowedOrigins { get; set; } = ["http://localhost:5173"];

    public int DefaultDepth { get; set; } = 5;

    public int DefaultGranulation { get; set; } = 1;
}
