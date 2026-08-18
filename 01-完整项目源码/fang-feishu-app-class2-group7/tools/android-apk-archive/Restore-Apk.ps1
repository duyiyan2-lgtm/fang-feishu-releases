[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [string]$OutputDirectory = (Get-Location).Path,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$manifestFile = Get-Item -LiteralPath $ManifestPath
$manifest = Get-Content -LiteralPath $manifestFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json

if ($manifest.format -ne 'fang-feishu-apk-chunks-v1') {
    throw "Unsupported archive format: $($manifest.format)"
}

$archiveRoot = Split-Path -Parent $manifestFile.Directory.FullName
$chunksRoot = Join-Path $archiveRoot 'chunks'
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$outputPath = Join-Path $outputRoot $manifest.fileName

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw "Output file already exists: $outputPath. Use -Force to replace it."
}

$target = [System.IO.File]::Open($outputPath, [System.IO.FileMode]::Create)
try {
    foreach ($part in $manifest.parts) {
        $chunkPath = Join-Path $chunksRoot ($part.path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $chunkPath)) {
            throw "Missing chunk: $chunkPath"
        }

        $source = [System.IO.File]::OpenRead($chunkPath)
        try {
            $source.CopyTo($target)
        }
        finally {
            $source.Dispose()
        }
    }
}
finally {
    $target.Dispose()
}

$actualSize = (Get-Item -LiteralPath $outputPath).Length
if ($actualSize -ne [long]$manifest.size) {
    Remove-Item -LiteralPath $outputPath -Force
    throw "File size check failed. Expected $($manifest.size), actual $actualSize."
}

$actualHash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash
if ($actualHash -ne $manifest.sha256) {
    Remove-Item -LiteralPath $outputPath -Force
    throw "SHA-256 check failed. Expected $($manifest.sha256), actual $actualHash."
}

Write-Host "APK restored and verified: $outputPath"
Write-Host "SHA-256: $actualHash"
