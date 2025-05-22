using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using App.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimaxServer.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Define the ports for the three server instances
        //int[] ports = { 5001};
        int[] ports = { 5001, 5002};
        List<Task> serverTasks = new List<Task>();

        foreach (int port in ports)
        {
            // Create a task for each server instance
            serverTasks.Add(Task.Run(() => StartServer(port)));
        }

        // Wait for all servers to complete (they should run indefinitely)
        await Task.WhenAll(serverTasks);
    }

    private static async Task StartServer(int port)
    {
        var builder = WebApplication.CreateBuilder(new[] { $"--urls=http://127.0.0.1:{port}" });

        // Configure Kestrel for this specific instance
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Parse("127.0.0.1"), port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
               // listenOptions.UseHttps(); // Use HTTPS for secure communication
            });

        });

        // Add services to the container
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<IBoardEvaluator, Evaluator>();

        // Add logging with instance identification
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.MapGrpcService<CheckersEvaluationServiceImpl>();
        app.MapGet("/", () => $"Checkers Minimax gRPC server is running on port {port}.");

        // Log server start
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"Starting server instance on port {port}");

        await app.RunAsync();
    }
}
