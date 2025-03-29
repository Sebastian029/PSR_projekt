using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GrpcService.Services;

namespace GrpcService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Dodane dla gRPC
            builder.Services.AddGrpc();



            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();
            app.UseCors();
            app.UseRouting();

            CheckersGame game = new CheckersGame();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<GreeterService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            // Endpoint WebSocket
            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await HandleWebSocket(webSocket, game);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            app.Run();
        }

        private static async Task HandleWebSocket(WebSocket webSocket, CheckersGame game)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received raw message: {message}");

                        try
                        {
                            if (message.Contains("\"type\":\"settings\""))
                            {
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                };

                                var settings = JsonSerializer.Deserialize<SettingsRequest>(message, options);
                                Console.WriteLine($"Received settings - Depth: {settings.Depth}, Granulation: {settings.Granulation}, performance: {settings.IsPerformanceTest}");

                                game.SetDifficulty(settings.Depth, settings.Granulation, settings.IsPerformanceTest);
                            }
                            else
                            {
                                var move = JsonSerializer.Deserialize<MoveRequest>(message);
                                Console.WriteLine("BACKEND - FROM " + move.from + "," + move.to);
                                if (move != null)
                                {
                                    bool success = true;
                                    GameStateResponse response;

                                    if (move.from == -1 && move.to == -1)
                                    {
                                        response = new GameStateResponse
                                        {
                                            Success = success,
                                            Board = game.GetBoardStateReset()
                                        };
                                    }
                                    else
                                    {
                                        success = game.PlayMove(move.from, move.to);
                                        response = new GameStateResponse
                                        {
                                            Success = success,
                                            Board = game.GetBoardState()
                                        };
                                    }

                                    string responseJson = JsonSerializer.Serialize(response);
                                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing message: {ex.Message}");
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
        }
    }

    // Klasy dla WebSocket
    public class MoveRequest
    {
        public int from { get; set; }
        public int to { get; set; }
    }

    public class GameStateResponse
    {
        public bool Success { get; set; }
        public string Board { get; set; }
    }

    public class SettingsRequest
    {
        [JsonPropertyName("depth")]
        public int Depth { get; set; }

        [JsonPropertyName("granulation")]
        public int Granulation { get; set; }

        [JsonPropertyName("isPerformanceTest")]
        public bool? IsPerformanceTest { get; set; }
    }
}