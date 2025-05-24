using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Server.WebSocketHandlers
{
    public class CheckersWebSocketHandler
    {
        private readonly CheckersGame _game;

        public CheckersWebSocketHandler(CheckersGame game)
        {
            _game = game;
        }

        public async Task HandleWebSocket(WebSocket webSocket, string socketId)
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
                        Console.WriteLine($"[{socketId}] Received: {message}");
                        await ProcessMessage(webSocket, message);
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

        private async Task ProcessMessage(WebSocket webSocket, string message)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(message);
                if (jsonDoc.RootElement.TryGetProperty("type", out var typeProperty))
                {
                    string messageType = typeProperty.GetString();
                    switch (messageType)
                    {
                        case "settings":
                            await HandleSettings(webSocket, message);
                            break;
                        case "move":
                            await HandlePlayerMove(webSocket, message);
                            break;
                        case "reset":
                            await HandleReset(webSocket);
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

        private async Task HandleSettings(WebSocket webSocket, string message)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
        
                var settings = JsonSerializer.Deserialize<GameSettings>(message, options);
                if (settings != null)
                {
                    Console.WriteLine($"Parsed settings: Depth={settings.Depth}, Granulation={settings.Granulation}, PerfTest={settings.IsPerformanceTest}, PlayerMode={settings.IsPlayerMode}");
            
                    _game.SetDifficulty(
                        settings.Depth,
                        settings.Granulation,
                        settings.IsPerformanceTest,
                        settings.IsPlayerMode
                    );
            
                    if (!settings.IsPlayerMode)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(100);
                            await StartComputerVsComputerGame(webSocket);
                        });
                    }

                    await SendGameState(webSocket);
                }
                else
                {
                    await SendError(webSocket, "Invalid settings format");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing settings: {ex.Message}");
                await SendError(webSocket, $"Error parsing settings: {ex.Message}");
            }
        }


        private async Task HandlePlayerMove(WebSocket webSocket, string message)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
        
                var move = JsonSerializer.Deserialize<PlayerMove>(message, options);
                if (move == null)
                {
                    await SendError(webSocket, "Invalid move format");
                    return;
                }

                Console.WriteLine($"Received move: FromRow={move.FromRow}, FromCol={move.FromCol}, ToRow={move.ToRow}, ToCol={move.ToCol}");

                // Walidacja współrzędnych
                if (move.FromRow < 0 || move.FromRow >= 8 || move.FromCol < 0 || move.FromCol >= 8 ||
                    move.ToRow < 0 || move.ToRow >= 8 || move.ToCol < 0 || move.ToCol >= 8)
                {
                    await SendError(webSocket, $"Invalid move coordinates: ({move.FromRow},{move.FromCol}) to ({move.ToRow},{move.ToCol})");
                    return;
                }

                if (_game.IsPlayerMode && !_game.IsWhiteTurn)
                {
                    await SendError(webSocket, "Not your turn - computer is playing");
                    return;
                }

                // Wykonaj ruch
                bool success = _game.PlayMove(move.FromRow, move.FromCol, move.ToRow, move.ToCol);
                if (!success)
                {
                    await SendError(webSocket, "Invalid move - check game rules");
                    return;
                }

                if (_game.IsPlayerMode && !_game.IsWhiteTurn && !_game.CheckGameOver())
                {
                    await Task.Delay(300);
                    await ProcessComputerTurn(webSocket);
                }

                await SendGameState(webSocket, success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing move: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendError(webSocket, $"Error processing move: {ex.Message}");
            }
        }



        private async Task ProcessComputerTurn(WebSocket webSocket)
        {
            bool success = false;
            var aiMove = _game.GetAIMove();
            if (aiMove.fromRow != -1)
            {
                success = _game.PlayMove(aiMove.fromRow, aiMove.fromCol, aiMove.toRow, aiMove.toCol);
                
                // Obsługa wielokrotnych bić
                while (success && _game.MustCaptureFrom.HasValue && !_game.IsWhiteTurn)
                {
                    var captures = _game.GetAllPossibleCaptures();
                    if (captures.TryGetValue(_game.MustCaptureFrom.Value, out var targets) && targets.Count > 0)
                    {
                        var captureTarget = targets[0];
                        success = _game.PlayMove(_game.MustCaptureFrom.Value.row, _game.MustCaptureFrom.Value.col, 
                                                captureTarget.row, captureTarget.col);
                        await SendGameState(webSocket, true);
                        if (!_game.IsPerformanceTest)
                            await Task.Delay(300);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            await SendGameState(webSocket, success);
        }

        private async Task StartComputerVsComputerGame(WebSocket webSocket)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!_game.CheckGameOver())
            {
                await ProcessComputerTurn(webSocket);
                if (!_game.IsPerformanceTest)
                    await Task.Delay(300);
            }
            stopwatch.Stop();
            GameLogger.LogGame(_game.Depth, _game.Granulation, stopwatch.ElapsedMilliseconds);
            await SendGameState(webSocket);
        }

        private async Task HandleReset(WebSocket webSocket)
        {
            _game.GetBoardStateReset();
            await SendGameState(webSocket);
            if (!_game.IsPlayerMode)
            {
                await StartComputerVsComputerGame(webSocket);
            }
        }

        private async Task SendGameState(WebSocket webSocket, bool? success = null)
        {
            var response = new GameStateResponse
            {
                Success = success ?? true,
                Board = _game.GetBoardState(),
                IsWhiteTurn = _game.IsWhiteTurn,
                GameOver = _game.CheckGameOver(),
                Winner = _game.CheckGameOver() ? (_game.HasWhiteWon() ? "white" : "black") : null,
                CurrentPlayer = _game.IsPlayerMode
                    ? (_game.IsWhiteTurn ? "human" : "computer")
                    : "computer"
            };

            await SendJson(webSocket, response);
        }

        private async Task SendJson(WebSocket webSocket, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendError(WebSocket webSocket, string error)
        {
            await SendJson(webSocket, new { error });
        }
    }

    // Klasy pomocnicze dla deserializacji JSON
    public class GameSettings
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    
        [JsonPropertyName("depth")]
        public int Depth { get; set; } = 5;
    
        [JsonPropertyName("granulation")]
        public int Granulation { get; set; } = 1;
    
        [JsonPropertyName("isPerformanceTest")]
        public bool IsPerformanceTest { get; set; } = false;
    
        [JsonPropertyName("isPlayerMode")]
        public bool IsPlayerMode { get; set; } = false;
    }

    public class PlayerMove
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        
        // Dla kompatybilności wstecznej z oryginalnym formatem
        public int From { get; set; }
        public int To { get; set; }
    }

    public class GameStateResponse
    {
        public bool Success { get; set; }
        public string Board { get; set; }
        public bool IsWhiteTurn { get; set; }
        public bool GameOver { get; set; }
        public string Winner { get; set; }
        public string CurrentPlayer { get; set; }
    }
}
