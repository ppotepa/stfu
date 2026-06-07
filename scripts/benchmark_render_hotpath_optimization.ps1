param(
    [string]$Project = "src/runtime/STFU.App/STFU.App.csproj",
    [string]$Asset = "assets/suzanne.obj",
    [int]$Width = 800,
    [int]$Height = 600,
    [int]$Frames = 60,
    [int]$Warmup = 5
)

$ErrorActionPreference = "Stop"

Write-Host "STFU render hot path optimization benchmark"
Write-Host "Project: $Project"
Write-Host "Asset:   $Asset"
Write-Host "Size:    ${Width}x${Height}"

dotnet build STFU.slnx -c Release

dotnet run --project $Project -c Release -- `
    --verify-render-parity default 320 240 3

dotnet run --project $Project -c Release -- `
    --bench-render-profiles $Asset $Width $Height $Frames default $Warmup --workers 1 --animation off

dotnet run --project $Project -c Release -- `
    --bench-render-profiles $Asset $Width $Height $Frames default $Warmup --workers 16 --animation off

dotnet run --project $Project -c Release -- `
    --smoke-gpu-present 320 240

dotnet run --project $Project -c Release -- `
    --smoke-gpu-readback 320 240
