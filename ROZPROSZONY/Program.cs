using Grpc.Core;
using Grpc.Net.Client;
using GrpcServer;
using System.Text.Json;

var channel = GrpcChannel.ForAddress("http://localhost:5168");
var client = new Greeter.GreeterClient(channel);

var request = new ClientInfo
{
    ClientName = "MojaAplikacja",
    DesiredRole = "player"
};

using var call = client.Subscribe(request);

await foreach (var update in call.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"ID: {update.ClientId}");
    Console.WriteLine($"Rola: {update.ClientRole}");
    Console.WriteLine($"Wiadomość: {update.Message}");

    if (!string.IsNullOrEmpty(update.CustomData))
    {
        var data = JsonSerializer.Deserialize<dynamic>(update.CustomData);
        Console.WriteLine($"Dane: {data}");
    }

    if (update.BinaryPayload != null && update.BinaryPayload.Length > 0)
    {
        Console.WriteLine($"Otrzymano dane binarne: {update.BinaryPayload.Length} bajtów");
    }
}