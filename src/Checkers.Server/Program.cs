using Checkers.Core.Ai;
using Checkers.Server.Configuration;
using Checkers.Server.Distribution;
using Checkers.Server.Sessions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection(ServerOptions.SectionName));

builder.Services.AddSingleton<IWorkerChannelFactory, WorkerChannelFactory>();
builder.Services.AddSingleton<WorkerPool>();
builder.Services.AddSingleton<LocalSearchExecutor>();
builder.Services.AddSingleton<IMetricsSink, CsvMetricsSink>();
builder.Services.AddSingleton<ISearchExecutor>(services =>
{
    var pool = services.GetRequiredService<WorkerPool>();
    var local = services.GetRequiredService<LocalSearchExecutor>();
    return pool.Workers.Count == 0
        ? local
        : new DistributedSearchExecutor(
            pool,
            local,
            services.GetRequiredService<IOptions<ServerOptions>>(),
            services.GetRequiredService<ILogger<DistributedSearchExecutor>>());
});

var allowedOrigins = builder.Configuration.GetSection(ServerOptions.SectionName)
    .GetSection(nameof(ServerOptions.AllowedOrigins)).Get<string[]>() ?? [];
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

app.MapGet("/", () => "Checkers game server. Connect a WebSocket client to /ws.");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/status", async (WorkerPool pool, ISearchExecutor executor, CancellationToken cancellationToken) =>
{
    var workers = await Task.WhenAll(pool.Workers.Select(async worker =>
    {
        try
        {
            var ping = await worker.PingAsync(TimeSpan.FromSeconds(2), cancellationToken);
            return new
            {
                address = worker.Address,
                reachable = true,
                name = (string?)ping.Worker,
                cores = ping.Cores,
                completedJobs = worker.CompletedJobs,
                failedJobs = worker.FailedJobs,
            };
        }
        catch (Exception ex) when (ex is Grpc.Core.RpcException or HttpRequestException or IOException)
        {
            return new
            {
                address = worker.Address,
                reachable = false,
                name = (string?)null,
                cores = 0,
                completedJobs = worker.CompletedJobs,
                failedJobs = worker.FailedJobs,
            };
        }
    }));

    return Results.Ok(new
    {
        mode = pool.Workers.Count == 0 ? "local" : "distributed",
        workers,
        remoteJobs = (executor as DistributedSearchExecutor)?.RemoteJobs ?? 0,
        fallbackJobs = (executor as DistributedSearchExecutor)?.FallbackJobs ?? 0,
    });
});

app.Map("/ws", async (
    HttpContext context,
    ISearchExecutor executor,
    IOptions<ServerOptions> options,
    IMetricsSink metrics,
    ILoggerFactory loggerFactory) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await using var session = new ClientSession(
        socket, executor, options, metrics, loggerFactory.CreateLogger<ClientSession>());
    await session.RunAsync(context.RequestAborted);
});

app.Run();

public partial class Program;
