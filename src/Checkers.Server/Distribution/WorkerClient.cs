using Checkers.Contracts;
using Checkers.Core.Ai;
using Grpc.Core;
using Grpc.Net.Client;

namespace Checkers.Server.Distribution;

public interface IWorkerChannelFactory
{
    GrpcChannel Create(string address);
}

public sealed class WorkerChannelFactory : IWorkerChannelFactory
{
    public GrpcChannel Create(string address) => GrpcChannel.ForAddress(address, new GrpcChannelOptions
    {
        HttpHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
        },
    });
}

public sealed class WorkerClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly SearchService.SearchServiceClient _client;
    private readonly TimeProvider _time;
    private long _unavailableUntil;
    private long _completedJobs;
    private long _failedJobs;

    public WorkerClient(string address, IWorkerChannelFactory channels, TimeProvider? time = null)
    {
        Address = address;
        _time = time ?? TimeProvider.System;
        _channel = channels.Create(address);
        _client = new SearchService.SearchServiceClient(_channel);
    }

    public string Address { get; }

    public long CompletedJobs => Interlocked.Read(ref _completedJobs);

    public long FailedJobs => Interlocked.Read(ref _failedJobs);

    public bool IsAvailable => _time.GetTimestamp() >= Interlocked.Read(ref _unavailableUntil);

    public async Task<SearchResult> SearchAsync(
        SearchJob job, string taskId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.SearchAsync(
                ProtoMapping.ToRequest(job, taskId),
                deadline: _time.GetUtcNow().UtcDateTime + timeout,
                cancellationToken: cancellationToken);

            Interlocked.Increment(ref _completedJobs);
            return ProtoMapping.ToResult(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Search cancelled by the caller.", ex, cancellationToken);
        }
    }

    public async Task<PingResponse> PingAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        await _client.PingAsync(
            new PingRequest(),
            deadline: _time.GetUtcNow().UtcDateTime + timeout,
            cancellationToken: cancellationToken);

    public void MarkFailed(TimeSpan cooldown)
    {
        Interlocked.Increment(ref _failedJobs);
        var until = _time.GetTimestamp() + (long)(cooldown.TotalSeconds * _time.TimestampFrequency);
        Interlocked.Exchange(ref _unavailableUntil, until);
    }

    public void Dispose() => _channel.Dispose();
}
