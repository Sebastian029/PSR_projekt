using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace GrpcService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Dodane dla gRPC
            builder.WebHost.ConfigureKestrel(options =>
            {
                // gRPC dzia�a na HTTP/2
                options.ListenAnyIP(5168, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });

                // WebSocket dzia�a na HTTP/1.1
                options.ListenAnyIP(5162, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
                });
            });
            CheckersGame game = new CheckersGame();
            builder.Services.AddSingleton(game);
            builder.Services.AddGrpc();
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
            app.UseCors("AllowAll");
            app.UseWebSockets();
            app.UseCors();
            app.UseRouting();

            

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
            var socketId = Guid.NewGuid().ToString();
            Console.WriteLine($"WebSocket connected: {socketId}");

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"[{socketId}] Received: {message}");

                        try
                        {
                            // Obsługa ustawień gry
                            if (message.Contains("\"type\":\"settings\""))
                            {
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                };

                                var settings = JsonSerializer.Deserialize<SettingsRequest>(message, options);
                                if (settings != null)
                                {
                                    game.SetDifficulty(
                                        settings.Depth,
                                        settings.Granulation,
                                        settings.IsPerformanceTest ?? false);

                                    await SendGameState(webSocket, game, true);
                                    await GreeterService.SendBoardUpdate(game);
                                }
                            }
                            // Obsługa ruchów
                            else
                            {
                                var move = JsonSerializer.Deserialize<MoveRequest>(message);
                                if (move != null)
                                {
                                    await ProcessMove(webSocket, game, move);
                                    await GreeterService.SendBoardUpdate(game);
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            Console.WriteLine($"JSON error: {jsonEx.Message}");
                            await SendError(webSocket, "Invalid message format");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Processing error: {ex.Message}");
                            await SendError(webSocket, "Processing error occurred");
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"[{socketId}] Closing connection");
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None);
                    }
                }
            }
            catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                Console.WriteLine($"[{socketId}] Client disconnected abruptly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{socketId}] Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"[{socketId}] Connection closed");
            }
        }

        // Pomocnicze metody
        private static async Task ProcessMove(WebSocket webSocket, CheckersGame game, MoveRequest move)
        {
            bool success;
            if (move.from == -1 && move.to == -1) // Reset gry
            {
                game.GetBoardStateReset();
                success = true;
            }
            else // Normalny ruch
            {
                success = game.PlayMove(move.from, move.to);

                if (success && !game.IsWhiteTurn && !game.CheckGameOver())
                {
                    success = await ProcessAITurn(game);
                }
            }

            await SendGameState(webSocket, game, success);
        }

        private static async Task<bool> ProcessAITurn(CheckersGame game)
        {
            var aiMove = game.GetAIMove();
            if (aiMove.fromField == -1) return false;

            bool success = game.PlayMove(aiMove.fromField, aiMove.toField);

            // Obsługa ciągłych bic
            while (success && game.MustCaptureFrom.HasValue)
            {
                var captures = game.GetAllPossibleCaptures();
                if (captures.TryGetValue(game.MustCaptureFrom.Value, out var targets) && targets.Count > 0)
                {
                    success = game.PlayMove(game.MustCaptureFrom.Value, targets[0]);
                }
                else
                {
                    break;
                }
            }

            return success;
        }

        private static async Task SendGameState(WebSocket webSocket, CheckersGame game, bool success)
        {
            var response = new GameStateResponse
            {
                Success = success,
                Board = game.GetBoardState(),
                IsWhiteTurn = game.IsWhiteTurn,
                GameOver = game.CheckGameOver(),
                Winner = game.CheckGameOver() ? (!game.IsWhiteTurn ? "white" : "black") : null
            };

            await SendJson(webSocket, response);
        }

        private static async Task SendJson(WebSocket webSocket, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        private static async Task SendError(WebSocket webSocket, string error)
        {
            await SendJson(webSocket, new { error });
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
    public bool? IsWhiteTurn { get; set; }
    public bool? GameOver { get; set; }
    public string Winner { get; set; }
    public string Error { get; set; }
    
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