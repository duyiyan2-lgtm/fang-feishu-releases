param()

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendRoot = Split-Path -Parent $scriptDir
$solutionPath = Join-Path $backendRoot 'FangFeishu.Backend.sln'

Write-Host 'Building backend solution...' -ForegroundColor Cyan
dotnet build $solutionPath

Write-Host 'Running backend tests...' -ForegroundColor Cyan
dotnet test $solutionPath --no-build

Write-Host 'Checks completed.' -ForegroundColor Green
