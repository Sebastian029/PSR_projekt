using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


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