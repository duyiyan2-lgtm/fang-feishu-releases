param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendRoot = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent $backendRoot
$composePath = Join-Path $repoRoot 'deploy\docker\docker-compose.dev.yml'

docker info | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'Docker Desktop is not running. Please start Docker Desktop and run this script again.'
}

if ($RemoveVolumes) {
    Write-Host 'Stopping FangFeishu backend stack and removing volumes...' -ForegroundColor Yellow
    docker compose -f $composePath down -v
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to stop Docker Compose stack.'
    }
}
else {
    Write-Host 'Stopping FangFeishu backend stack...' -ForegroundColor Cyan
    docker compose -f $composePath down
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to stop Docker Compose stack.'
    }
}
