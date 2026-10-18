namespace Checkers.Core.Ai;

public interface ISearchExecutor
{
    Task<SearchResult[]> ExecuteAsync(IReadOnlyList<SearchJob> jobs, CancellationToken cancellationToken = default);
}

public sealed class LocalSearchExecutor : ISearchExecutor
{
    private readonly int _maxParallelism;

    public LocalSearchExecutor(int? maxParallelism = null)
    {
        _maxParallelism = maxParallelism ?? Environment.ProcessorCount;
        ArgumentOutOfRangeException.ThrowIfLessThan(_maxParallelism, 1);
    }

    public async Task<SearchResult[]> ExecuteAsync(
        IReadOnlyList<SearchJob> jobs, CancellationToken cancellationToken = default)
    {
        var results = new SearchResult[jobs.Count];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForAsync(0, jobs.Count, options, (index, token) =>
        {
            var job = jobs[index];
            results[index] = MinimaxSearcher.Search(job.Board, job.SideToMove, job.Depth, token);
            return ValueTask.CompletedTask;
        });

        return results;
    }
}
