[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$SkipBench
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

& powershell -NoProfile -File (Join-Path $root "tools\ci\guard-parallelism.ps1")
& dotnet test (Join-Path $root "tests\STFU.Parallelism.Tests\STFU.Parallelism.Tests.csproj") -c $Configuration -v minimal
& dotnet test (Join-Path $root "tests\STFU.Rendering.Abstractions.Tests\STFU.Rendering.Abstractions.Tests.csproj") -c $Configuration -v minimal
& dotnet test (Join-Path $root "tests\STFU.Rendering.Cpu.Tests\STFU.Rendering.Cpu.Tests.csproj") -c $Configuration -v minimal
& dotnet test (Join-Path $root "tests\STFU.NPR.Parity.Tests\STFU.NPR.Parity.Tests.csproj") -c $Configuration -v minimal

if (-not $SkipBench) {
    Write-Host ""
    Write-Host "Benchmarks are not automated in this script."
    Write-Host "Run the render/profile commands from docs/performance/threading-baseline.md when needed."
}
