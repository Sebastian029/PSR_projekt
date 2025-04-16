// using Grpc.Core;
// using GrpcServer;
// using System.Collections.Concurrent;
// using System.Text.Json;
//
// public class GreeterService : Greeter.GreeterBase
// {
//     private readonly ILogger<GreeterService> _logger;
//     private static readonly ConcurrentDictionary<string, (IServerStreamWriter<PersonalizedUpdate>, string)> _clients = new();
//     private static int _clientCounter = 0;
//
//     public override async Task Subscribe(ClientInfo request,
//         IServerStreamWriter<PersonalizedUpdate> responseStream,
//         ServerCallContext context)
//     {
//         // Generowanie unikalnego ID i roli
//         var clientId = GenerateClientId();
//         var clientRole = AssignRole(request.DesiredRole);
//
//         // Rejestracja klienta
//         _clients.TryAdd(clientId, (responseStream, clientRole));
//
//         try
//         {
//             // Wysyłanie wiadomości powitalnej
//             await SendWelcomeMessage(clientId, clientRole, request.ClientName);
//
//             // Utrzymywanie połączenia
//             while (!context.CancellationToken.IsCancellationRequested)
//             {
//                 await Task.Delay(1000, context.CancellationToken);
//             }
//         }
//         finally
//         {
//             _clients.TryRemove(clientId, out _);
//             await NotifyClientDisconnected(clientId);
//         }
//     }
//
//     private string GenerateClientId()
//     {
//         return $"client_{Interlocked.Increment(ref _clientCounter)}_{Guid.NewGuid().ToString("N")[..4]}";
//     }
//
//     private string AssignRole(string desiredRole)
//     {
//         // Logika przypisywania ról - można rozbudować
//         return string.IsNullOrEmpty(desiredRole) ?
//             "default_role" :
//             desiredRole.ToLower();
//     }
//
//     private async Task SendWelcomeMessage(string clientId, string role, string clientName)
//     {
//         var welcomeMsg = new PersonalizedUpdate
//         {
//             ClientId = clientId,
//             ClientRole = role,
//             Message = $"Witaj {(string.IsNullOrEmpty(clientName) ? "Anonymous" : clientName)}!",
//             CustomData = JsonSerializer.Serialize(new
//             {
//                 AssignedAt = DateTime.UtcNow,
//                 YourRole = role,
//                 OtherClients = _clients.Count
//             })
//         };
//
//         await SendToClient(clientId, welcomeMsg);
//     }
//
//     public static async Task SendToClient(string clientId, PersonalizedUpdate update)
//     {
//         if (_clients.TryGetValue(clientId, out var client))
//         {
//             try
//             {
//                 await client.Item1.WriteAsync(update);
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Error sending to {clientId}: {ex.Message}");
//                 _clients.TryRemove(clientId, out _);
//             }
//         }
//     }   
//
//     public static async Task SendToRole(string role, Action<PersonalizedUpdate> configureUpdate)
//     {
//         var update = new PersonalizedUpdate();
//         configureUpdate(update);
//
//         foreach (var client in _clients.Where(c => c.Value.Item2 == role))
//         {
//             await SendToClient(client.Key, update);
//         }
//     }
//
//     public static async Task Broadcast(Action<PersonalizedUpdate> configureUpdate)
//     {
//         var update = new PersonalizedUpdate();
//         configureUpdate(update);
//
//         foreach (var client in _clients)
//         {
//             await SendToClient(client.Key, update);
//         }
//     }
//
//     private async Task NotifyClientDisconnected(string clientId)
//     {
//         // Można powiadomić innych klientów o rozłączeniu
//         await Broadcast(update => {
//             update.Message = $"Client {clientId} disconnected";
//             update.CustomData = JsonSerializer.Serialize(new
//             {
//                 DisconnectedClient = clientId,
//                 Timestamp = DateTime.UtcNow
//             });
//         });
//     }
// }