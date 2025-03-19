using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS if needed for your frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseWebSockets(); // Enable WebSocket support

// Apply CORS policy globally
app.UseCors();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        try
        {
            using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocket(webSocket);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting WebSocket: {ex.Message}");
            context.Response.StatusCode = 500; // Internal Server Error
        }
    }
    else
    {
        context.Response.StatusCode = 400; // Bad Request
    }
});

async Task HandleWebSocket(WebSocket webSocket)
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
          

                // Process the received move (you can update the game state here)

                string response = "SERVER: " + message;
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling WebSocket: {ex.Message}");
    }
}

app.Run("http://localhost:5000");

