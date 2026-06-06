param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Generator = 'Visual Studio 17 2022',

    [string]$Platform = 'x64',

    [string]$BuildDirectory = '',

    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$sourceDir = Join-Path $repoRoot 'src/native/STFU.Native.Fbx'
$buildDir = if ([string]::IsNullOrWhiteSpace($BuildDirectory)) {
    Join-Path $repoRoot 'artifacts/native/STFU.Native.Fbx-build'
} else {
    [System.IO.Path]::GetFullPath($BuildDirectory)
}
$outputDir = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $repoRoot 'artifacts/native/STFU.Native.Fbx'
} else {
    [System.IO.Path]::GetFullPath($OutputDirectory)
}

if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
    throw 'cmake was not found on PATH.'
}

New-Item -ItemType Directory -Force $buildDir | Out-Null
New-Item -ItemType Directory -Force $outputDir | Out-Null

cmake -S $sourceDir -B $buildDir -G $Generator -A $Platform
cmake --build $buildDir --config $Configuration

$candidatePaths = @(
    (Join-Path $buildDir "$Configuration/stfu_fbx.dll"),
    (Join-Path $buildDir 'stfu_fbx.dll')
)

$dll = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $dll) {
    throw "Could not find stfu_fbx.dll under $buildDir."
}

$target = Join-Path $outputDir 'stfu_fbx.dll'
Copy-Item -LiteralPath $dll -Destination $target -Force
Write-Host "Built native FBX DLL: $target"
