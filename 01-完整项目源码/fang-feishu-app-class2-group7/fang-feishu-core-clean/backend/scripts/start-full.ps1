param(
    [switch]$Reset
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendRoot = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent $backendRoot
$composePath = Join-Path $repoRoot 'deploy\docker\docker-compose.dev.yml'
$healthUrl = 'http://127.0.0.1:5080/health'

docker info | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'Docker Desktop is not running. Please start Docker Desktop and run this script again.'
}

if ($Reset) {
    Write-Host 'Resetting full stack containers and volumes...' -ForegroundColor Yellow
    docker compose -f $composePath down -v
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to reset Docker Compose stack.'
    }
}

Write-Host 'Starting FangFeishu backend stack...' -ForegroundColor Cyan
docker compose -f $composePath up -d --build
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to start Docker Compose stack.'
}

Write-Host 'Waiting for API health check...' -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds(90)
do {
    try {
        $response = Invoke-RestMethod -Method Get -Uri $healthUrl -TimeoutSec 3
        if ($response.status -eq 'Healthy') {
            Write-Host 'FangFeishu backend stack is ready.' -ForegroundColor Green
            Write-Host "API:     http://127.0.0.1:5080"
            Write-Host "Swagger: http://127.0.0.1:5080/swagger"
            Write-Host "MinIO:   http://127.0.0.1:9001  (minioadmin / minioadmin)"
            exit 0
        }
    }
    catch {
        Start-Sleep -Seconds 2
    }
} while ((Get-Date) -lt $deadline)

Write-Host 'API did not become healthy in time. Showing container status:' -ForegroundColor Red
docker compose -f $composePath ps
exit 1
