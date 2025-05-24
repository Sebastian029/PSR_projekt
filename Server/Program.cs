using System;
using System.Collections.Generic;
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
        int[] ports = { 5001, 5002 };
        List<Task> serverTasks = new List<Task>();

        foreach (int port in ports)
        {
            serverTasks.Add(Task.Run(() => StartServer(port)));
        }

        await Task.WhenAll(serverTasks);
    }

    private static async Task StartServer(int port)
    {
        var builder = WebApplication.CreateBuilder(new[] { $"--urls=http://127.0.0.1:{port}" });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Parse("127.0.0.1"), port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        // POPRAWKA: Dodaj rejestrację serwisów
        builder.Services.AddGrpc();
        
        // WAŻNE: Zarejestruj IBoardEvaluator z właściwym namespace
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