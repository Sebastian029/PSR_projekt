using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Checkers.Core.Ai;
using Checkers.Core.Game;
using Checkers.Core.Model;
using Checkers.Server.Configuration;
using Checkers.Server.Distribution;
using Microsoft.Extensions.Options;

namespace Checkers.Server.Sessions;

public sealed class ClientSession : IAsyncDisposable
{
    private const int MaxMessageBytes = 16 * 1024;

    private readonly WebSocket _socket;
    private readonly AiPlayer _ai;
    private readonly ServerOptions _options;
    private readonly IMetricsSink _metrics;
    private readonly ILogger _logger;
    private readonly string _gameId = Guid.NewGuid().ToString("N")[..8];
    private readonly CheckersGame _game = new();
    private readonly SemaphoreSlim _gameLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();

    private GameSettings _settings;
    private CancellationTokenSource? _aiCts;
    private Task _aiTask = Task.CompletedTask;
    private int _generation;
    private volatile bool _aiBusy;
    private AiMoveStats? _lastAi;

    public ClientSession(
        WebSocket socket,
        ISearchExecutor executor,
        IOptions<ServerOptions> options,
        IMetricsSink metrics,
        ILogger<ClientSession> logger)
    {
        _socket = socket;
        _ai = new AiPlayer(executor, Random.Shared);
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;
        _settings = new GameSettings(_options.DefaultDepth, _options.DefaultGranulation, false, true);
    }

    public string GameId => _gameId;

    public async Task RunAsync(CancellationToken requestAborted)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestAborted, _lifetime.Token);
        var token = linked.Token;
        _logger.LogInformation("Game {GameId}: client connected", _gameId);

        try
        {
            while (_socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var text = await ReceiveTextAsync(token);
                if (text is null)
                {
                    break;
                }

                await HandleMessageAsync(text, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "Game {GameId}: socket error", _gameId);
        }
        finally
        {
            _logger.LogInformation("Game {GameId}: client disconnected", _gameId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifetime.CancelAsync();
        await StopAiAsync();
        _lifetime.Dispose();
        _gameLock.Dispose();
        _sendLock.Dispose();
    }

    private async Task HandleMessageAsync(string text, CancellationToken token)
    {
        ClientMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<ClientMessage>(text, Json.Read);
        }
        catch (JsonException)
        {
            await SendAsync(new ErrorMessage("Message is not valid JSON."), token);
            return;
        }

        if (message is null || string.IsNullOrEmpty(message.Type))
        {
            await SendAsync(new ErrorMessage("Message type not specified."), token);
            return;
        }

        switch (message.Type)
        {
            case "settings":
                await HandleSettingsAsync(message, token);
                break;
            case "move":
                await HandleMoveAsync(message, token);
                break;
            case "reset":
                await HandleResetAsync(token);
                break;
            default:
                await SendAsync(new ErrorMessage($"Unknown message type '{message.Type}'."), token);
                break;
        }
    }

    private async Task HandleSettingsAsync(ClientMessage message, CancellationToken token)
    {
        var settings = new GameSettings(
            message.Depth ?? _options.DefaultDepth,
            message.Granulation ?? _options.DefaultGranulation,
            message.IsPerformanceTest ?? false,
            message.IsPlayerMode ?? true);

        if (settings.Depth is < 1 or > SearchOptions.MaxDepth || settings.Granulation < 1)
        {
            await SendAsync(
                new ErrorMessage($"Depth must be 1-{SearchOptions.MaxDepth} and granulation at least 1."), token);
            return;
        }

        await StopAiAsync();
        await _gameLock.WaitAsync(token);
        try
        {
            _settings = settings;
            _game.Reset();
            _lastAi = null;
            _generation++;
        }
        finally
        {
            _gameLock.Release();
        }

        _logger.LogInformation(
            "Game {GameId}: new game, depth {Depth}, granulation {Granulation}, {Mode}",
            _gameId, settings.Depth, settings.Granulation, settings.IsPlayerMode ? "human vs computer" : "computer vs computer");

        await SendStateAsync(true, token);
        StartAiIfNeeded();
    }

    private async Task HandleResetAsync(CancellationToken token)
    {
        await StopAiAsync();
        await _gameLock.WaitAsync(token);
        try
        {
            _game.Reset();
            _lastAi = null;
            _generation++;
        }
        finally
        {
            _gameLock.Release();
        }

        await SendStateAsync(true, token);
        StartAiIfNeeded();
    }

    private async Task HandleMoveAsync(ClientMessage message, CancellationToken token)
    {
        string? error = null;
        await _gameLock.WaitAsync(token);
        try
        {
            if (!_settings.IsPlayerMode)
            {
                error = "Moves are not accepted in computer vs computer mode.";
            }
            else if (_aiBusy || _game.SideToMove != Side.White)
            {
                error = "It is not your turn.";
            }
            else
            {
                var result = _game.TryPlayStep(
                    new Square(message.FromRow, message.FromCol), new Square(message.ToRow, message.ToCol));
                if (!result.Success)
                {
                    error = result.Error;
                }
            }
        }
        finally
        {
            _gameLock.Release();
        }

        if (error is not null)
        {
            await SendAsync(new ErrorMessage(error), token);
            return;
        }

        await SendStateAsync(true, token);
        StartAiIfNeeded();
    }

    private void StartAiIfNeeded()
    {
        if (_game.IsOver || _lifetime.IsCancellationRequested)
        {
            return;
        }

        var computerToMove = !_settings.IsPlayerMode || _game.SideToMove == Side.Black;
        if (!computerToMove)
        {
            return;
        }

        if (_aiTask.IsCompleted)
        {
            _aiCts?.Dispose();
        }

        _aiBusy = true;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _aiCts = cts;
        var generation = _generation;
        _aiTask = Task.Run(() => RunAiAsync(generation, cts), CancellationToken.None);
    }

    private async Task RunAiAsync(int generation, CancellationTokenSource source)
    {
        var token = source.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var more = await PlayComputerTurnAsync(generation, token);
                if (!more)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game {GameId}: computer turn failed", _gameId);
            try
            {
                await SendAsync(new ErrorMessage("The computer could not complete its move."), CancellationToken.None);
            }
            catch (Exception sendError) when (sendError is WebSocketException or ObjectDisposedException)
            {
            }
        }
        finally
        {
            if (ReferenceEquals(_aiCts, source))
            {
                _aiBusy = false;
            }
        }
    }

    private async Task<bool> PlayComputerTurnAsync(int generation, CancellationToken token)
    {
        Board board;
        Side side;
        Square? mustContinue;
        GameSettings settings;

        await _gameLock.WaitAsync(token);
        try
        {
            if (generation != _generation || _game.IsOver)
            {
                return false;
            }

            board = _game.Board.Clone();
            side = _game.SideToMove;
            mustContinue = _game.MustContinueFrom;
            settings = _settings;
        }
        finally
        {
            _gameLock.Release();
        }

        var best = await _ai.FindBestMoveAsync(board, side, settings.ToSearchOptions(), mustContinue, token);
        if (best is null)
        {
            return false;
        }

        if (!settings.IsPerformanceTest && _options.AiMoveDelayMs > 0)
        {
            await Task.Delay(_options.AiMoveDelayMs, token);
        }

        bool keepPlaying;
        await _gameLock.WaitAsync(token);
        try
        {
            if (generation != _generation)
            {
                return false;
            }

            _game.PlayMove(best.Move);
            _lastAi = new AiMoveStats((long)best.Elapsed.TotalMilliseconds, best.Nodes, best.Jobs, best.Score, best.WasForced);
            keepPlaying = !_game.IsOver && (!settings.IsPlayerMode || _game.SideToMove == Side.Black);
            if (!keepPlaying)
            {
                _aiBusy = false;
            }
        }
        finally
        {
            _gameLock.Release();
        }

        _metrics.Record(new MoveMetrics(
            _gameId, side.ToString(), settings.Depth, settings.Granulation, best.Jobs, best.Nodes,
            (long)best.Elapsed.TotalMilliseconds, best.Score, best.WasForced, _options.Workers.Count));

        await SendStateAsync(true, token);
        return keepPlaying;
    }

    private async Task StopAiAsync()
    {
        var cts = _aiCts;
        if (cts is null)
        {
            return;
        }

        await cts.CancelAsync();
        try
        {
            await _aiTask;
        }
        catch (OperationCanceledException)
        {
        }

        cts.Dispose();
        _aiCts = null;
        _aiBusy = false;
    }

    private async Task SendStateAsync(bool success, CancellationToken token)
    {
        GameStateMessage state;
        await _gameLock.WaitAsync(token);
        try
        {
            state = GameStateMessage.From(_game, _settings.IsPlayerMode, success, _lastAi);
        }
        finally
        {
            _gameLock.Release();
        }

        await SendAsync(state, token);
    }

    private async Task SendAsync<T>(T payload, CancellationToken token)
    {
        if (_socket.State != WebSocketState.Open)
        {
            return;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json.Write);
        await _sendLock.WaitAsync(token);
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, token);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<string?> ReceiveTextAsync(CancellationToken token)
    {
        var buffer = new byte[2048];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await _socket.ReceiveAsync(buffer, token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (_socket.State == WebSocketState.CloseReceived)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }

                return null;
            }

            message.Write(buffer, 0, result.Count);
            if (message.Length > MaxMessageBytes)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message too large", CancellationToken.None);
                return null;
            }

            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            }
        }
    }
}
