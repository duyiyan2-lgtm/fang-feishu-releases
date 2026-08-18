param(
    [switch]$RecreateContainers = $true
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendRoot = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent $backendRoot
$composePath = Join-Path $repoRoot 'deploy\docker\docker-compose.dev.yml'
$apiProject = Join-Path $backendRoot 'src\FangFeishu.Api\FangFeishu.Api.csproj'

docker info | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'Docker Desktop is not running. Please start Docker Desktop and run this script again.'
}

if ($RecreateContainers) {
    Write-Host 'Recreating dev containers...' -ForegroundColor Cyan
    docker compose -f $composePath down -v
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to reset Docker Compose services.'
    }
    docker compose -f $composePath up -d postgres minio
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to start PostgreSQL and MinIO services.'
    }
}

Write-Host 'Applying EF Core migrations...' -ForegroundColor Cyan
dotnet ef database update --project $apiProject --startup-project $apiProject

Write-Host 'Dev environment is ready.' -ForegroundColor Green
