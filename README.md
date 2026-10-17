# Distributed Checkers

An 8x8 draughts game whose AI (minimax with alpha-beta pruning) can spread its search over several machines.
A browser client talks to a game server over WebSocket; the server splits the search tree into independent
jobs and sends them to worker processes over gRPC. If a worker is slow, crashes or is unreachable, its jobs go
to the other workers or are computed locally, so a move is always produced.

Built for a distributed-systems course (PSR), then reworked into a tested, runnable project.

```
 browser (React)  --WebSocket-->  Checkers.Server  --gRPC-->  Checkers.Worker  (one or many)
                                  game + AI coordinator       evaluates sub-trees
                                          \__ same engine (Checkers.Core) is used on both sides __/
```

## Quick start

Requirements: [.NET SDK 8+](https://dotnet.microsoft.com/download) and Node.js 20+.

```powershell
./scripts/run-all.ps1            # 2 workers + server + client   (Linux/macOS: ./scripts/run-all.sh)
```

Open <http://localhost:5173>, choose *single player* and play white against the computer.
`./scripts/run-all.ps1 -Workers 0` starts only the server; the AI then uses the local CPU cores.

Or start the pieces yourself:

```bash
dotnet run --project src/Checkers.Worker                     # gRPC worker on :5001
dotnet run --project src/Checkers.Server                     # game server on :5162
cd client && npm install && npm run dev                      # web client on :5173
```

Point the server at workers (any number, any machines) with configuration or environment variables:

```bash
Checkers__Workers__0=http://192.168.1.20:5001 Checkers__Workers__1=http://192.168.1.21:5001 \
  dotnet run --project src/Checkers.Server
```

A worker on another machine must listen on a reachable address, for example
`Kestrel__Endpoints__Grpc__Url=http://0.0.0.0:5001`. Check the setup at <http://localhost:5162/api/status>.

## Repository layout

| Path | Contents |
|---|---|
| `src/Checkers.Core` | Rules, move generation, evaluation, minimax, game state machine. No I/O, no framework dependencies. |
| `src/Checkers.Contracts` | `search.proto` (the server-worker contract) and mapping to engine types. |
| `src/Checkers.Server` | ASP.NET Core app: WebSocket API, one game session per connection, worker pool, CSV metrics. |
| `src/Checkers.Worker` | ASP.NET Core gRPC service that evaluates one position to a given depth. |
| `client/` | React + Vite UI. |
| `tests/Checkers.Core.Tests` | Unit tests of rules, search and game flow. |
| `tests/Checkers.IntegrationTests` | In-memory gRPC workers, the distributed executor and the WebSocket API end to end. |
| `tools/Checkers.Benchmark` | Compares local and distributed move search. |
| `scripts/` | Start-everything scripts. |
| `docs/ARCHITECTURE.md` | Design notes, protocol, failure handling. |

## Rules implemented

White moves first and moves up the board. Men move and capture forward only; kings are "flying" (any distance).
Capturing is mandatory (any capture, there is no majority rule) and a piece keeps capturing until it cannot.
A man reaching the last row becomes a king and its turn ends. A player with no legal move loses.
A game is drawn on threefold repetition or after 80 plies without a capture or man move.

## Configuration (`Checkers` section of `src/Checkers.Server/appsettings.json`)

| Key | Default | Meaning |
|---|---|---|
| `Workers` | `[]` | Worker gRPC addresses. Empty = local search. |
| `ConcurrencyPerWorker` | 4 | Jobs in flight per worker. |
| `WorkerTimeoutSeconds` | 120 | Deadline for one job. |
| `WorkerCooldownSeconds` | 15 | How long a failed worker is skipped. |
| `AiMoveDelayMs` | 600 | Pause before the computer moves (not in performance-test mode). |
| `MetricsCsvPath` | `logs/search_metrics.csv` | One CSV row per computer move; empty disables it. |
| `AllowedOrigins` | `http://localhost:5173` | CORS origins for the HTTP API. |

Client: set `VITE_WS_URL` (see `client/.env.example`) if the server is not on `ws://localhost:5162/ws`.

## Tests

```bash
dotnet test Checkers.sln                  # 91 tests
cd client && npm run lint && npm run build
```

Notable checks: alpha-beta returns exactly the same value as plain minimax on random positions; the score is
mirror-symmetric when colours are swapped (so odd and even depths behave alike); the move chosen over gRPC equals
the move chosen locally for every granulation; the server still plays when workers are down; every board square
survives the wire format.

## Benchmark

```bash
dotnet run -c Release --project tools/Checkers.Benchmark -- --depth 11 --granulation 3 --positions 2 \
    --workers http://127.0.0.1:5001,http://127.0.0.1:5002
```

Measured on one 12-core Windows machine with two workers on localhost (depth 11, granulation 3):
local 3.6 s versus 4.0 s with workers (0.89x overall, 1.05x on the better position), identical moves.
On one box the workers and the local search compete for the same cores, so no speed-up is expected there;
the architecture pays off when workers run on separate machines. That scenario has not been measured here.

## What changed compared with the course version

The original code is kept as the git tag `legacy-minmax-fix-v16`. Problems found and fixed during the rework:

- The board was packed into 3 integers (24 of 32 squares), so remote workers silently searched a different position.
- The worker ignored mandatory captures and multi-captures that the game itself enforced.
- The evaluation flipped its sign with the parity of the search depth; odd depths played the worst move.
- With granulation above 1, results were folded with a single min/max instead of alternating up the tree.
- Server addresses were hard-coded to two private IPs; a dead worker hung the whole game (no timeout, no retry).
- One game object was shared by every browser connection.
- The client replaced its state with defaults whenever the server sent an error, and showed draws as "Black wins".
- Three duplicated copies of the AI code (server, worker, an unused third project) were merged into `Checkers.Core`.

## Limitations

- Fixed evaluation function and no opening book or quiescence search; strength is modest.
- Workers are configured statically; there is no discovery and no authentication on the gRPC port (plain HTTP/2),
  so run it on a trusted network only.
- The human always plays white; no move history or undo.
