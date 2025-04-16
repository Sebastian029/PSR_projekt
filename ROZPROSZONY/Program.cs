using Grpc.Core;
using Grpc.Net.Client;
using GrpcServer;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


// var channel = GrpcChannel.ForAddress("http://localhost:5168");
// var client = new Greeter.GreeterClient(channel);
//
// var request = new ClientInfo
// {
//     ClientName = "MojaAplikacja",
//     DesiredRole = "player"
// };
//
// using var call = client.Subscribe(request);
//
// await foreach (var update in call.ResponseStream.ReadAllAsync())
// {
//     Console.WriteLine($"ID: {update.ClientId}");
//     Console.WriteLine($"Rola: {update.ClientRole}");
//     Console.WriteLine($"Wiadomość: {update.Message}");
//
//     if (!string.IsNullOrEmpty(update.CustomData))
//     {
//         var data = JsonSerializer.Deserialize<dynamic>(update.CustomData);
//         Console.WriteLine($"Dane: {data}");
//     }
//
//     if (update.BinaryPayload != null && update.BinaryPayload.Length > 0)
//     {
//         Console.WriteLine($"Otrzymano dane binarne: {update.BinaryPayload.Length} bajtów");
//     }
// }

namespace GrpcService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Existing setup code...

            // CheckersGame game = new CheckersGame();
            // builder.Services.AddSingleton(game);
            builder.Services.AddGrpc();
            var app = builder.Build();

            // Existing middleware...

            // Configure the HTTP request pipeline.

            app.MapGrpcService<CheckersServiceImpl>(); // Register the new service
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            // Existing WebSocket setup...

            app.Run();
        }

        // Existing methods...
    }
}