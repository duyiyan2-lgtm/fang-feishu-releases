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

if ($manifest.format -ne 'fang-feishu-file-chunks-v1') {
    throw "Unsupported archive format: $($manifest.format)"
}

$archiveRoot = $manifestFile.Directory.FullName
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$outputPath = Join-Path $outputRoot $manifest.fileName
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw "Output file already exists: $outputPath. Use -Force to replace it."
}

$target = [System.IO.File]::Open($outputPath, [System.IO.FileMode]::Create)
try {
    foreach ($part in ($manifest.parts | Sort-Object index)) {
        $partPath = Join-Path $archiveRoot ($part.path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $partPath)) {
            throw "Missing part: $partPath"
        }
        $partHash = (Get-FileHash -LiteralPath $partPath -Algorithm SHA256).Hash
        if ($partHash -ne $part.sha256) {
            throw "Part checksum failed: $partPath"
        }
        $source = [System.IO.File]::OpenRead($partPath)
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
$actualHash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash
if ($actualSize -ne [long]$manifest.size -or $actualHash -ne $manifest.sha256) {
    Remove-Item -LiteralPath $outputPath -Force
    throw "Final file verification failed."
}

Write-Host "File restored and verified: $outputPath"
Write-Host "SHA-256: $actualHash"
