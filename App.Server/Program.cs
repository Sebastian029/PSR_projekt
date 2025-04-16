using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GrpcServer;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace GrpcService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5168, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
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

            CheckersGame game = new CheckersGame();
            builder.Services.AddSingleton(game);
            builder.Services.AddGrpc();

            var app = builder.Build();

            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
            app.UseCors("AllowAll");
            app.UseWebSockets();
            app.UseRouting();
            app.MapGet("/", () => "Checkers Game Server");

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
                            var jsonDoc = JsonDocument.Parse(message);
                            if (jsonDoc.RootElement.TryGetProperty("type", out var typeProperty))
                            {
                                string messageType = typeProperty.GetString();
                                switch (messageType)
                                {
                                    case "settings":
                                        await HandleSettings(webSocket, game, message);
                                        break;
                                    case "move":
                                        Console.WriteLine("Jeden");
                                        await HandlePlayerMove(webSocket, game, message);
                                        break;
                                    case "reset":
                                        await HandleReset(webSocket, game);
                                        break;
                                    default:
                                        await SendError(webSocket, "Unknown message type");
                                        break;
                                }
                            }
                            else
                            {
                                await SendError(webSocket, "Message type not specified");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing message: {ex.Message}");
                            await SendError(webSocket, "Error processing message");
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"WebSocket disconnected: {socketId}");
            }
        }

        private static async Task HandleSettings(WebSocket webSocket, CheckersGame game, string message)
        {
            var settings = JsonSerializer.Deserialize<SettingsRequest>(message);
            if (settings != null)
            {
                game.SetDifficulty(
                    settings.Depth,
                    settings.Granulation,
                    settings.IsPerformanceTest,
                    settings.IsPlayerMode
                );

                if (!settings.IsPlayerMode)
                {
                    // Rozpocznij automatyczną grę w trybie komputer vs komputer
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(100); // Krótkie opóźnienie na inicjalizację
                        await StartComputerVsComputerGame(webSocket, game);
                    });
                }

                await SendGameState(webSocket, game);
            }
            else
            {
                await SendError(webSocket, "Invalid settings format");
            }
        }

        private static async Task HandlePlayerMove(WebSocket webSocket, CheckersGame game, string message)
        {
            Console.Write("Wszedłem");
            var move = JsonSerializer.Deserialize<MoveRequest>(message);
            if (move == null)
            {
                await SendError(webSocket, "Invalid move format");
                return;
            }

            // Sprawdź czy to właściwa tura gracza
            if (game.IsPlayerMode && !game.IsWhiteTurn)
            {
                await SendError(webSocket, "Not your turn - computer is playing");
                return;
            }

            bool success = game.PlayMove(move.from, move.to);
            if (!success)
            {
                await SendError(webSocket, "Invalid move");
                return;
            }

            // W trybie gracz vs komputer, po ruchu gracza wykonaj ruch komputera
            if (game.IsPlayerMode && !game.IsWhiteTurn && !game.CheckGameOver())
            {
                await Task.Delay(300); // Krótkie opóźnienie dla lepszego UX
                await ProcessComputerTurn(webSocket, game);
            }

            await SendGameState(webSocket, game, true);
        }

        private static async Task ProcessComputerTurn(WebSocket webSocket, CheckersGame game)
        {
            bool success = false;
            var aiMove = game.GetAIMove();
            if (aiMove.fromField != -1)
            {
                success = game.PlayMove(aiMove.fromField, aiMove.toField);

                // Obsługa ciągłych bic tylko w turze komputera
                while (success && game.MustCaptureFrom.HasValue && !game.IsWhiteTurn)
                {
                    var captures = game.GetAllPossibleCaptures();
                    if (captures.TryGetValue(game.MustCaptureFrom.Value, out var targets) && targets.Count > 0)
                    {
                        success = game.PlayMove(game.MustCaptureFrom.Value, targets[0]);
                        await SendGameState(webSocket, game, true);
                        await Task.Delay(300);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            await SendGameState(webSocket, game, success);
        }

        private static async Task StartComputerVsComputerGame(WebSocket webSocket, CheckersGame game)
        {
            while (!game.CheckGameOver())
            {
                await ProcessComputerTurn(webSocket, game);
                await Task.Delay(500); // Opóźnienie między ruchami dla lepszej widoczności
            }
            await SendGameState(webSocket, game);
        }

        private static async Task HandleReset(WebSocket webSocket, CheckersGame game)
        {
            game.GetBoardStateReset();
            await SendGameState(webSocket, game);

            if (!game.IsPlayerMode)
            {
                await StartComputerVsComputerGame(webSocket, game);
            }
        }

        private static async Task SendGameState(WebSocket webSocket, CheckersGame game, bool? success = null)
        {
            var response = new GameStateResponse
            {
                Success = success ?? true,
                Board = game.GetBoardState(),
                IsWhiteTurn = game.IsWhiteTurn,
                GameOver = game.CheckGameOver(),
                Winner = game.CheckGameOver() ? (game.HasWhiteWon() ? "white" : "black") : null,
                CurrentPlayer = game.IsPlayerMode
                    ? (game.IsWhiteTurn ? "human" : "computer")
                    : "computer"
            };
            await SendJson(webSocket, response);
        }

        private static async Task SendJson(WebSocket webSocket, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task SendError(WebSocket webSocket, string error)
        {
            await SendJson(webSocket, new { error });
        }
    }

    public class MoveRequest
    {
        public string type { get; set; } = "move";
        public int from { get; set; }
        public int to { get; set; }
    }

    public class GameStateResponse
    {
        public bool Success { get; set; }
        public string Board { get; set; }
        public bool IsWhiteTurn { get; set; }
        public bool GameOver { get; set; }
        public string Winner { get; set; }
        public string CurrentPlayer { get; set; } // "human" lub "computer"
    }

    public class SettingsRequest
    {
        public string type { get; set; } = "settings";

        [JsonPropertyName("depth")]
        public int Depth { get; set; }

        [JsonPropertyName("granulation")]
        public int Granulation { get; set; }

        [JsonPropertyName("isPerformanceTest")]
        public bool IsPerformanceTest { get; set; }

        // Add GameMode setting
        [JsonPropertyName("isPlayerMode")]
        public bool IsPlayerMode { get; set; }
    }

    public class ResetRequest
    {
        public string type { get; set; } = "reset";
    }
}