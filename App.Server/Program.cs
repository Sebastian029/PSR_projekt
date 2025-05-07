using System.Net.WebSockets;
using App.Server.WebSocketHandlers;
using GrpcService;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace App.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5000, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                options.ListenAnyIP(5162, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .WithExposedHeaders("Grpc-Status", "Grpc-Message");
                });
            });

            builder.Services.AddSingleton<CheckersGame>();
            builder.Services.AddSingleton<CheckersWebSocketHandler>();
            
            builder.Services.AddGrpc();
            builder.Services.AddSingleton<WorkerCoordinator>();
            var app = builder.Build();

            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
            app.UseCors("AllowAll");
            app.UseWebSockets();
            app.UseRouting();
            app.MapGrpcService<CheckersServiceImpl>();
            app.MapGet("/", () => "Checkers Game Server");

            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var handler = context.RequestServices.GetRequiredService<CheckersWebSocketHandler>();
                    var socketId = Guid.NewGuid().ToString();
                    Console.WriteLine($"WebSocket connected: {socketId}");
                    await handler.HandleWebSocket(webSocket, socketId);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            app.Run();
        }
    }
}