#!/usr/bin/env bash
# Starts N search workers, the game server and the web client for local development.
#   ./scripts/run-all.sh [workers=2] [--no-client]
set -euo pipefail

workers="${1:-2}"
no_client=false
[[ "${2:-}" == "--no-client" || "${1:-}" == "--no-client" ]] && no_client=true
[[ "${1:-}" == "--no-client" ]] && workers=2

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
pids=()
cleanup() { for pid in "${pids[@]:-}"; do kill "$pid" 2>/dev/null || true; done; }
trap cleanup EXIT INT TERM

dotnet build "$root/Checkers.sln" -c Release --nologo -v quiet

addresses=()
for ((i = 0; i < workers; i++)); do
  port=$((5001 + i))
  addresses+=("http://127.0.0.1:$port")
  (cd "$root/src/Checkers.Worker" &&
    Kestrel__Endpoints__Grpc__Url="http://127.0.0.1:$port" Worker__Name="worker-$((i + 1))" \
      exec dotnet bin/Release/net8.0/Checkers.Worker.dll) &
  pids+=($!)
done

server_env=()
for i in "${!addresses[@]}"; do server_env+=("Checkers__Workers__$i=${addresses[$i]}"); done
(cd "$root/src/Checkers.Server" && exec env "${server_env[@]}" dotnet bin/Release/net8.0/Checkers.Server.dll) &
pids+=($!)

if [[ "$no_client" == false ]]; then
  [[ -d "$root/client/node_modules" ]] || npm --prefix "$root/client" install
  npm --prefix "$root/client" run dev &
  pids+=($!)
fi

echo "Server: http://localhost:5162   Client: http://localhost:5173   Workers: ${addresses[*]:-none}"
wait
