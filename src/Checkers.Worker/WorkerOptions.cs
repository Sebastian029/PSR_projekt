namespace Checkers.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>Name reported to the coordinator; defaults to the machine name.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Searches running at once. Extra requests wait in a queue instead of oversubscribing the CPU.
    /// Defaults to the number of logical cores.
    /// </summary>
    public int? MaxConcurrentSearches { get; set; }

    public string ResolvedName => string.IsNullOrWhiteSpace(Name) ? Environment.MachineName : Name;

    public int ResolvedConcurrency => Math.Max(1, MaxConcurrentSearches ?? Environment.ProcessorCount);
}
