using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Checkers.Server.Sessions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Checkers.IntegrationTests;

public sealed class GameServerTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly string _metricsPath = Path.Combine(Path.GetTempPath(), $"checkers-metrics-{Guid.NewGuid():N}.csv");
    private readonly List<WebApplicationFactory<ClientSession>> _factories = [];

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }

        if (File.Exists(_metricsPath))
        {
            File.Delete(_metricsPath);
        }
    }

    private WebApplicationFactory<ClientSession> CreateServer(params string[] workers)
    {
        var factory = new WebApplicationFactory<ClientSession>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Checkers:AiMoveDelayMs", "0");
            builder.UseSetting("Checkers:MetricsCsvPath", _metricsPath);
            for (var i = 0; i < workers.Length; i++)
            {
                builder.UseSetting($"Checkers:Workers:{i}", workers[i]);
            }
        });
        _factories.Add(factory);
        return factory;
    }

    private static async Task<GameClient> ConnectAsync(WebApplicationFactory<ClientSession> factory)
    {
        var client = factory.Server.CreateWebSocketClient();
        var socket = await client.ConnectAsync(new Uri(factory.Server.BaseAddress, "/ws"), CancellationToken.None);
        return new GameClient(socket);
    }

    [Fact]
    public async Task HumanMoveIsFollowedByAComputerReply()
    {
        using var client = await ConnectAsync(CreateServer());

        await client.SendAsync(new { type = "settings", depth = 2, granulation = 1, isPerformanceTest = true, isPlayerMode = true });
        var start = await client.ReceiveAsync();
        Assert.True(start.GetProperty("IsWhiteTurn").GetBoolean());
        Assert.Equal("human", start.GetProperty("CurrentPlayer").GetString());
        Assert.Equal(64, start.GetProperty("Board").GetArrayLength());

        await client.SendAsync(new { type = "move", FromRow = 5, FromCol = 2, ToRow = 4, ToCol = 3 });
        var afterHuman = await client.ReceiveAsync();
        Assert.False(afterHuman.GetProperty("IsWhiteTurn").GetBoolean());
        Assert.Equal("computer", afterHuman.GetProperty("CurrentPlayer").GetString());

        var afterComputer = await client.ReceiveAsync();
        Assert.True(afterComputer.GetProperty("IsWhiteTurn").GetBoolean());
        Assert.Equal("human", afterComputer.GetProperty("CurrentPlayer").GetString());
        Assert.NotEqual(afterHuman.GetProperty("Board").ToString(), afterComputer.GetProperty("Board").ToString());
        Assert.True(afterComputer.GetProperty("LastAiMove").GetProperty("Jobs").GetInt32() >= 0);
    }

    [Fact]
    public async Task IllegalMoveGetsAnErrorAndLeavesTheGameAlone()
    {
        using var client = await ConnectAsync(CreateServer());
        await client.SendAsync(new { type = "settings", depth = 1, granulation = 1, isPlayerMode = true });
        await client.ReceiveAsync();

        await client.SendAsync(new { type = "move", FromRow = 5, FromCol = 2, ToRow = 2, ToCol = 5 });
        var error = await client.ReceiveAsync();

        Assert.True(error.TryGetProperty("error", out var message));
        Assert.False(string.IsNullOrWhiteSpace(message.GetString()));
    }

    [Fact]
    public async Task InvalidSettingsAndUnknownMessagesAreRejected()
    {
        using var client = await ConnectAsync(CreateServer());

        await client.SendAsync(new { type = "settings", depth = 0, granulation = 1 });
        Assert.True((await client.ReceiveAsync()).TryGetProperty("error", out _));

        await client.SendAsync(new { type = "nonsense" });
        Assert.True((await client.ReceiveAsync()).TryGetProperty("error", out _));

        await client.SendRawAsync("this is not json");
        Assert.True((await client.ReceiveAsync()).TryGetProperty("error", out _));
    }

    [Fact]
    public async Task ComputerVersusComputerPlaysToTheEndAndWritesMetrics()
    {
        using var client = await ConnectAsync(CreateServer());

        await client.SendAsync(new { type = "settings", depth = 2, granulation = 1, isPerformanceTest = true, isPlayerMode = false });

        JsonElement last;
        var states = 0;
        do
        {
            last = await client.ReceiveAsync();
            states++;
        }
        while (!last.GetProperty("GameOver").GetBoolean());

        Assert.True(states > 3);
        Assert.Contains(last.GetProperty("Winner").GetString(), new[] { "white", "black", "draw" });
        Assert.True(File.Exists(_metricsPath));
        var lines = await File.ReadAllLinesAsync(_metricsPath);
        Assert.StartsWith("timestamp_utc,", lines[0]);
        Assert.True(lines.Length > 2);
    }

    [Fact]
    public async Task ResetInterruptsARunningGame()
    {
        using var client = await ConnectAsync(CreateServer());
        await client.SendAsync(new { type = "settings", depth = 4, granulation = 1, isPerformanceTest = true, isPlayerMode = false });
        await client.ReceiveAsync();

        await client.SendAsync(new { type = "reset" });

        for (var i = 0; i < 50; i++)
        {
            var state = await client.ReceiveAsync();
            if (state.GetProperty("Board").GetArrayLength() == 64
                && state.GetProperty("IsWhiteTurn").GetBoolean()
                && !state.GetProperty("GameOver").GetBoolean()
                && state.GetProperty("Board")[1].GetString() == "black"
                && state.GetProperty("Board")[40].GetString() == "white")
            {
                return;
            }
        }

        Assert.Fail("Never received a fresh board after reset.");
    }

    [Fact]
    public async Task UnreachableWorkerDoesNotStopTheGame()
    {
        using var client = await ConnectAsync(CreateServer("http://127.0.0.1:1"));

        await client.SendAsync(new { type = "settings", depth = 3, granulation = 1, isPerformanceTest = true, isPlayerMode = true });
        await client.ReceiveAsync();
        await client.SendAsync(new { type = "move", FromRow = 5, FromCol = 2, ToRow = 4, ToCol = 3 });
        await client.ReceiveAsync();

        var reply = await client.ReceiveAsync();

        Assert.True(reply.GetProperty("IsWhiteTurn").GetBoolean());
        Assert.False(reply.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task StatusEndpointReportsTheWorkerMode()
    {
        using var http = CreateServer("http://127.0.0.1:1").CreateClient();

        var json = await http.GetStringAsync("/api/status");
        using var document = JsonDocument.Parse(json);

        Assert.Equal("distributed", document.RootElement.GetProperty("mode").GetString());
        Assert.False(document.RootElement.GetProperty("workers")[0].GetProperty("reachable").GetBoolean());
    }

    private sealed class GameClient(WebSocket socket) : IDisposable
    {
        public Task SendAsync(object payload) => SendRawAsync(JsonSerializer.Serialize(payload));

        public Task SendRawAsync(string text) => socket.SendAsync(
            Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

        public async Task<JsonElement> ReceiveAsync()
        {
            using var cts = new CancellationTokenSource(Timeout);
            var buffer = new byte[16 * 1024];
            using var message = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cts.Token);
                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            return JsonDocument.Parse(message.ToArray()).RootElement.Clone();
        }

        public void Dispose() => socket.Dispose();
    }
}
