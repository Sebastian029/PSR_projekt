using Checkers.Server.Configuration;
using Microsoft.Extensions.Options;

namespace Checkers.Server.Distribution;

public sealed class WorkerPool : IDisposable
{
    public WorkerPool(IOptions<ServerOptions> options, IWorkerChannelFactory channels, TimeProvider? time = null)
    {
        Workers = options.Value.Workers
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(address => new WorkerClient(address, channels, time))
            .ToList();
    }

    public IReadOnlyList<WorkerClient> Workers { get; }

    public List<WorkerClient> AvailableWorkers() => Workers.Where(worker => worker.IsAvailable).ToList();

    public void Dispose()
    {
        foreach (var worker in Workers)
        {
            worker.Dispose();
        }
    }
}
