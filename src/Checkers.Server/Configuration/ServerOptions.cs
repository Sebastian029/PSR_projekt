namespace Checkers.Server.Configuration;

public sealed class ServerOptions
{
    public const string SectionName = "Checkers";

    /// <summary>
    /// gRPC addresses of search workers, e.g. <c>http://192.168.1.20:5001</c>.
    /// Empty = the server searches on its own CPU cores.
    /// </summary>
    public List<string> Workers { get; set; } = [];

    /// <summary>Jobs a single worker handles at the same time.</summary>
    public int ConcurrencyPerWorker { get; set; } = 4;

    /// <summary>Deadline for one job on a worker. A worker missing it is treated as failed.</summary>
    public int WorkerTimeoutSeconds { get; set; } = 120;

    /// <summary>How long a failed worker is skipped before it is tried again.</summary>
    public int WorkerCooldownSeconds { get; set; } = 15;

    /// <summary>Pause before the computer plays, so a human can follow the game. Ignored in performance-test mode.</summary>
    public int AiMoveDelayMs { get; set; } = 600;

    /// <summary>Per-move search metrics are appended here as CSV. Empty disables metrics.</summary>
    public string? MetricsCsvPath { get; set; } = "logs/search_metrics.csv";

    /// <summary>Browser origins allowed to call the HTTP API (the WebSocket is not subject to CORS).</summary>
    public List<string> AllowedOrigins { get; set; } = ["http://localhost:5173"];

    public int DefaultDepth { get; set; } = 5;

    public int DefaultGranulation { get; set; } = 1;
}
