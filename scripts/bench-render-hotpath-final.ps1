param(
    [string]$Project = "src/runtime/STFU.App/STFU.App.csproj",
    [string]$Asset = "assets\suzanne.obj",
    [int]$Width = 800,
    [int]$Height = 600,
    [int]$Frames = 60,
    [string]$Preset = "default",
    [int]$Warmup = 5
)

$ErrorActionPreference = "Stop"

Write-Host "Building STFU release..."
dotnet build STFU.slnx -c Release

Write-Host "Running baseline worker=1 benchmark..."
dotnet run --project $Project -c Release -- --bench-render-profiles $Asset $Width $Height $Frames $Preset $Warmup --workers 1

Write-Host "Running parallel worker=16 benchmark..."
dotnet run --project $Project -c Release -- --bench-render-profiles $Asset $Width $Height $Frames $Preset $Warmup --workers 16

Write-Host "Running parity guard..."
dotnet run --project $Project -c Release -- --verify-render-parity default 320 240 3

Write-Host "Running GPU present/readback smoke guards..."
dotnet run --project $Project -c Release -- --smoke-gpu-present 320 240
dotnet run --project $Project -c Release -- --smoke-gpu-readback 320 240
