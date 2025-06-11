using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimaxServer.Services;
using MinimaxServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        await StartServer(5001);
    }

    private static async Task StartServer(int port)
    {
        var builder = WebApplication.CreateBuilder(new[] { $"--urls=http://0.0.0.0:{port}" });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        // Register services
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<MinimaxServer.IBoardEvaluator, MinimaxServer.Evaluator>();

        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        var app = builder.Build();

        app.MapGrpcService<CheckersEvaluationServiceImpl>();
        app.MapGet("/", () => $"Checkers Minimax gRPC server is running on port {port}.");

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"Starting server instance on port {port}");

        await app.RunAsync();
    }
}