<#
.SYNOPSIS
  Starts the whole stack for local development: N search workers, the game server and the web client.

.EXAMPLE
  ./scripts/run-all.ps1                 # 2 workers + server + client
  ./scripts/run-all.ps1 -Workers 0      # server only: the AI searches on its own cores
  ./scripts/run-all.ps1 -NoClient       # backend only

Press Ctrl+C to stop everything.
#>
param(
    [int]$Workers = 2,
    [int]$FirstWorkerPort = 5001,
    [switch]$NoClient
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$processes = @()

try {
    dotnet build (Join-Path $root 'Checkers.sln') -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    $addresses = @()
    for ($i = 0; $i -lt $Workers; $i++) {
        $port = $FirstWorkerPort + $i
        $addresses += "http://127.0.0.1:$port"
        $env:Kestrel__Endpoints__Grpc__Url = "http://127.0.0.1:$port"
        $env:Worker__Name = "worker-$($i + 1)"
        $processes += Start-Process dotnet -ArgumentList (Join-Path $root 'src/Checkers.Worker/bin/Release/net8.0/Checkers.Worker.dll') `
            -WorkingDirectory (Join-Path $root 'src/Checkers.Worker') -PassThru -NoNewWindow
    }
    Remove-Item Env:Kestrel__Endpoints__Grpc__Url -ErrorAction SilentlyContinue
    Remove-Item Env:Worker__Name -ErrorAction SilentlyContinue

    for ($i = 0; $i -lt $addresses.Count; $i++) {
        Set-Item "Env:Checkers__Workers__$i" $addresses[$i]
    }
    $processes += Start-Process dotnet -ArgumentList (Join-Path $root 'src/Checkers.Server/bin/Release/net8.0/Checkers.Server.dll') `
        -WorkingDirectory (Join-Path $root 'src/Checkers.Server') -PassThru -NoNewWindow

    if (-not $NoClient) {
        $client = Join-Path $root 'client'
        if (-not (Test-Path (Join-Path $client 'node_modules'))) {
            npm --prefix $client install
        }
        $processes += Start-Process npm -ArgumentList '--prefix', $client, 'run', 'dev' -PassThru -NoNewWindow
    }

    Write-Host ''
    Write-Host 'Server : http://localhost:5162  (status: /api/status)'
    if (-not $NoClient) { Write-Host 'Client : http://localhost:5173' }
    Write-Host "Workers: $($addresses -join ', ')"
    Write-Host 'Press Ctrl+C to stop.'
    Wait-Process -Id ($processes | ForEach-Object Id)
}
finally {
    foreach ($process in $processes) {
        if ($process -and -not $process.HasExited) {
            # /T also stops child processes (npm -> node).
            taskkill /PID $process.Id /T /F | Out-Null
        }
    }
}
