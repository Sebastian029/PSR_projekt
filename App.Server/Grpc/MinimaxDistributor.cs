using App.Grpc;
using App.Server;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace App.Client;

/// <summary>
/// Sends one minimax job to every server, waits for the reply, then immediately
/// refills the now-idle server with the next queued job.  
/// This keeps every worker busy and removes the “slowest host dictates the batch
/// time” problem that the simple round-robin implementation had.
/// </summary>
public sealed class MinimaxDistributor : IDisposable
{
    private readonly List<string> _serverAddresses;
    private readonly Dictionary<string, GrpcChannel> _channels;

    public MinimaxDistributor(List<string> serverAddresses)
    {
        if (serverAddresses is null || serverAddresses.Count == 0)
            throw new ArgumentException("At least one server address is required", nameof(serverAddresses));

        _serverAddresses = serverAddresses;

        var channelOptions = new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(60),
                KeepAlivePingDelay          = TimeSpan.FromSeconds(600),
                KeepAlivePingTimeout        = TimeSpan.FromSeconds(300),
                EnableMultipleHttp2Connections = true
            }
        };

        _channels = serverAddresses.ToDictionary(
            a => a,
            a => GrpcChannel.ForAddress(a, channelOptions));
    }

    #region public API
    /// <summary>
    /// Dynamically load-balances <paramref name="allTasks"/> over every registered
    /// server.  
    /// Each server always has at most ONE in-flight request. As soon as it
    /// replies, the next job is dequeued and sent to that same server.
    /// </summary>
    public async Task<IReadOnlyList<int>> ProcessTasksLoadBalanced(
        List<(CheckersBoard board, int depth, bool isMaximizing)> allTasks)
    {
        if (allTasks is null) throw new ArgumentNullException(nameof(allTasks));
        if (allTasks.Count == 0) return Array.Empty<int>();

        var results = new int[allTasks.Count];

        // global job queue
        var pending = new ConcurrentQueue<(int idx, CheckersBoard board, int depth, bool max)>();
        for (int i = 0; i < allTasks.Count; i++)
            pending.Enqueue((i, allTasks[i].board, allTasks[i].depth, allTasks[i].isMaximizing));

        int finished = 0;
        var allDone  = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // one long-running worker per server
        var workers = _serverAddresses.Select(address => Task.Run(async () =>
        {
            var client  = new CheckersEvaluationService.CheckersEvaluationServiceClient(_channels[address]);

            // give the server its *first* job immediately (if any left)
            while (pending.TryDequeue(out var job))
            {
                var score = await SendJobAsync(client, address, job);

                results[job.idx] = score;

                if (Interlocked.Increment(ref finished) == allTasks.Count)
                {
                    allDone.TrySetResult();
                    return; // nothing left to do
                }
            }
        })).ToList();

        // wait until every result slot has been filled
        await allDone.Task;
        await Task.WhenAll(workers);

        return results;
    }
    #endregion

    #region internals
    private async Task<int> SendJobAsync(
        CheckersEvaluationService.CheckersEvaluationServiceClient client,
        string serverAddress,
        (int idx, CheckersBoard board, int depth, bool max) job)
    {
        string taskId = Guid.NewGuid().ToString();
        var swTotal   = Stopwatch.StartNew();
        var swConv    = Stopwatch.StartNew();

        // ---- build protobuf request ------------------------------------------------
        var req = new MinimaxRequest
        {
            Depth         = job.depth,
            IsMaximizing  = job.max,
            RequestTime   = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            TaskId        = taskId
        };

        var compressed = ConvertBoardTo32Format(job.board);
        req.Board.Add(compressed[0]);
        req.Board.Add(compressed[1]);
        req.Board.Add(compressed[2]);

        swConv.Stop();

        // ---- call remote -----------------------------------------------------------
        var swNet = Stopwatch.StartNew();
        var reply = await client.MinimaxSearchAsync(req);
        swNet.Stop();
        swTotal.Stop();

        Console.WriteLine(
            $"Server {serverAddress} - Task {job.idx} (ID: {taskId}) " +
            $"completed in {swTotal.ElapsedMilliseconds} ms with score {reply.Score}");

        GameLogger.LogMinimaxOperation(
            "DISTRIBUTED",
            serverAddress,
            job.depth,
            job.max,
            swTotal.ElapsedMilliseconds,
            swConv.ElapsedMilliseconds,
            swNet.ElapsedMilliseconds,
            reply.ServerComputationTimeMs,
            reply.Score);

        return reply.Score;
    }

    private static uint[] ConvertBoardTo32Format(CheckersBoard board)
    {
        var result = ArrayPool<uint>.Shared.Rent(3);
        Array.Clear(result, 0, 3);

        int field = 0;
        for (int r = 0; r < 8; r++)
        for (int c = 0; c < 8; c++)
        {
            if (((r + c) & 1) == 0) continue;           // skip light squares
            if (field >= 32) break;

            byte val = board.GetPiece(r, c) switch
            {
                PieceType.Empty       => 0,
                PieceType.WhitePawn   => 1,
                PieceType.WhiteKing   => 2,
                PieceType.BlackPawn   => 3,
                PieceType.BlackKing   => 4,
                _                     => 0
            };

            int idx = field / 8;
            int bit = (field % 8) * 4;
            uint mask = 0xFu << bit;
            result[idx] = (result[idx] & ~mask) | ((uint)val << bit);

            field++;
        }

        return result;
    }
    #endregion

    public void Dispose()
    {
        foreach (var ch in _channels.Values)
            ch.Dispose();

        _channels.Clear();
    }
}
