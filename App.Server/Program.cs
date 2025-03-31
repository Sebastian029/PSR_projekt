using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using App.Server;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseWebSockets();
app.UseCors();

CheckersGame game = new CheckersGame(); 

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

async Task HandleWebSocket(WebSocket webSocket, CheckersGame game)
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
                Console.WriteLine("AI: " + game.GetAIMove());

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

                        // Tutaj ustawiamy warto�ci w grze
                        game.SetDifficulty(settings.Depth, settings.Granulation, settings.IsPerformanceTest);

                    }
                    else { 
                    var move = JsonSerializer.Deserialize<MoveRequest>(message);
                    Console.WriteLine("BACKEND - FROM " +  move.from + "," + move.to);
                    if (move != null)
                    {
                        bool success = true; 

                        GameStateResponse response;

                        if (move.from == -1 && move.to == -1)
                        {
                            // Reset game
                            response = new GameStateResponse
                            {
                                Success = success,
                                Board = game.GetBoardStateReset(),
                                IsWhiteTurn = true,
                                GameOver = false
                            };
                        }
                        else
                        {
                            // Process player move
                            success = game.PlayMove(move.from, move.to);
                            
                            
                            
                            // Check if game is over after player move
                            bool gameOver = game.CheckGameOver();
                            
                            // If game isn't over and it's now black's turn, make AI move
                            if (success && !gameOver && !game.IsWhiteTurn)
                            {
                                var aiMove = game.GetAIMove();
                                if (aiMove.fromField == -1) break; // No valid move
                                    
                                success = game.PlayMove(aiMove.fromField, aiMove.toField);
                                    
                                // If the AI made a capture, check for follow-up captures
                                while (success && game.MustCaptureFrom.HasValue)
                                {
                                    var followUpCaptures = game.GetAllPossibleCaptures();
                                    if (followUpCaptures.TryGetValue(game.MustCaptureFrom.Value, out var targets) && targets.Count > 0)
                                    {
                                        // AI picks the first available follow-up capture
                                        var nextCapture = targets[0];
                                        success = game.PlayMove(game.MustCaptureFrom.Value, nextCapture);
                                    }
                                    else
                                    {
                                        break; // No more captures
                                    }
                                }
                            }
                            
                            response = new GameStateResponse
                            {
                                Success = success,
                                Board = game.GetBoardState(),
                                IsWhiteTurn = game.IsWhiteTurn,
                                GameOver = gameOver,
                                Winner = gameOver ? (!game.IsWhiteTurn ? "white" : "black") : null
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

app.Run();

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