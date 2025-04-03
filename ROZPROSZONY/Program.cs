using Grpc.Core;
using Grpc.Net.Client;
using GrpcServer;

class Program
{
    static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5168");
        var client = new Greeter.GreeterClient(channel);

        Console.WriteLine("Subskrybuję aktualizacje planszy...");
        Console.WriteLine("Naciśnij Ctrl+C aby zakończyć...");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => cts.Cancel();

        try
        {
            var subscription = client.SubscribeToBoardUpdates(
                new SubscriptionRequest { ClientId = Guid.NewGuid().ToString() },
                cancellationToken: cts.Token);

            await foreach (var update in subscription.ResponseStream.ReadAllAsync(cancellationToken: cts.Token))
            {
                Console.Clear();
                Console.WriteLine($"Ostatnia aktualizacja: {DateTime.Now:T}");
                Console.WriteLine($"Plansza:\n{update.BoardState.Replace(",", "\n")}");
                Console.WriteLine($"Tura białych: {update.IsWhiteTurn}");
                Console.WriteLine($"Status gry: {(update.GameOver ? "Zakończona" : "W trakcie")}");
                if (update.GameOver)
                {
                    Console.WriteLine($"Zwycięzca: {update.Winner}");
                }
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Console.WriteLine("Subskrypcja anulowana.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }
}