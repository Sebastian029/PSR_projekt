using Grpc.Core;
using GrpcServer;
using System.Collections.Concurrent;

public class GreeterService : GrpcServer.Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    private readonly CheckersGame _game;
    private static readonly ConcurrentDictionary<string, IServerStreamWriter<BoardUpdate>> _subscribers = new();

    public GreeterService(ILogger<GreeterService> logger, CheckersGame game)
    {
        _logger = logger;
        _game = game;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
    }

    public override async Task SubscribeToBoardUpdates(SubscriptionRequest request,
        IServerStreamWriter<BoardUpdate> responseStream,
        ServerCallContext context)
    {
        var clientId = request.ClientId ?? Guid.NewGuid().ToString();
        _subscribers.TryAdd(clientId, responseStream);

        try
        {
            // Wysyłamy początkowy stan planszy
            await SendBoardUpdate(_game);

            // Utrzymujemy połączenie otwarte
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, context.CancellationToken);
            }
        }
        finally
        {
            _subscribers.TryRemove(clientId, out _);
        }
    }

    public static async Task SendBoardUpdate(CheckersGame game)
    {
        var update = new BoardUpdate
        {
            BoardState = game.GetBoardState() ?? "",  // Ensure non-null
            IsWhiteTurn = game.IsWhiteTurn,
            GameOver = game.CheckGameOver(),
            Winner = ""  // Default empty string
        };

        // Only set winner if game is over
        if (update.GameOver)
        {
            update.Winner = !game.IsWhiteTurn ? "white" : "black";
        }

        foreach (var subscriber in _subscribers.ToArray())
        {
            try
            {
                await subscriber.Value.WriteAsync(update);
                Console.WriteLine($"Sent update to client {subscriber.Key}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending to client {subscriber.Key}: {ex.Message}");
                _subscribers.TryRemove(subscriber.Key, out _);
            }
        }
    }
}