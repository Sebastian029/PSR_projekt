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
                //Console.WriteLine($"Received raw message: {message}");


                try
                {
                    var move = JsonSerializer.Deserialize<MoveRequest>(message);
                    Console.WriteLine("BACKEND - FROM " +  move.from + "," + move.to);
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
}
