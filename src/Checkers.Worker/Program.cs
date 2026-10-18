using Checkers.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));
builder.Services.AddSingleton<SearchGate>();
builder.Services.AddGrpc(options => options.MaxReceiveMessageSize = 64 * 1024);

var app = builder.Build();

app.MapGrpcService<SearchServiceImpl>();
app.MapGet("/", () => "Checkers search worker (gRPC over HTTP/2). Use a gRPC client.");

app.Run();

public partial class Program;
