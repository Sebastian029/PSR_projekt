using System.Net.WebSockets;
using System.Text;
using System.Text.Json;


namespace GrpcService.WebSocketHandlers
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
            var settings = JsonSerializer.Deserialize<SettingsRequest>(message);
            if (settings != null)
            {
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

        private async Task HandlePlayerMove(WebSocket webSocket, string message)
        {
            var move = JsonSerializer.Deserialize<MoveRequest>(message);
            if (move == null)
            {
                await SendError(webSocket, "Invalid move format");
                return;
            }

            if (_game.IsPlayerMode && !_game.IsWhiteTurn)
            {
                await SendError(webSocket, "Not your turn - computer is playing");
                return;
            }

            bool success = _game.PlayMove(move.from, move.to);
            if (!success)
            {
                await SendError(webSocket, "Invalid move");
                return;
            }

            if (_game.IsPlayerMode && !_game.IsWhiteTurn && !_game.CheckGameOver())
            {
                await Task.Delay(300);
                await ProcessComputerTurn(webSocket);
            }

            await SendGameState(webSocket, success);
        }

        private async Task ProcessComputerTurn(WebSocket webSocket)
        {
            bool success = false;
            var aiMove = _game.GetAIMove();
            if (aiMove.fromField != -1)
            {
                success = _game.PlayMove(aiMove.fromField, aiMove.toField);

                while (success && _game.MustCaptureFrom.HasValue && !_game.IsWhiteTurn)
                {
                    var captures = _game.GetAllPossibleCaptures();
                    if (captures.TryGetValue(_game.MustCaptureFrom.Value, out var targets) && targets.Count > 0)
                    {
                        success = _game.PlayMove(_game.MustCaptureFrom.Value, targets[0]);
                        await SendGameState(webSocket, true);
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
            while (!_game.CheckGameOver())
            {
                await ProcessComputerTurn(webSocket);
                await Task.Delay(500);
            }
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
}