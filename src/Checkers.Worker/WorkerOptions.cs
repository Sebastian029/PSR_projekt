namespace Checkers.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public string? Name { get; set; }

    public int? MaxConcurrentSearches { get; set; }

    public string ResolvedName => string.IsNullOrWhiteSpace(Name) ? Environment.MachineName : Name;

    public int ResolvedConcurrency => Math.Max(1, MaxConcurrentSearches ?? Environment.ProcessorCount);
}
