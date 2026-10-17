using Microsoft.Extensions.Options;

namespace Checkers.Worker;

public sealed class SearchGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    public SearchGate(IOptions<WorkerOptions> options) =>
        _semaphore = new SemaphoreSlim(options.Value.ResolvedConcurrency);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                semaphore.Release();
            }
        }
    }
}
