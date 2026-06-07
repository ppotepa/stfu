param(
    [string]$Configuration = "Release",
    [string]$Asset = "assets\walking.fbx"
)

$ErrorActionPreference = "Stop"

Write-Host "STFU NPR final optimization validation"
Write-Host "Configuration: $Configuration"
Write-Host "Asset: $Asset"

dotnet build STFU.slnx -c $Configuration

dotnet test tests/STFU.Parallelism.Tests/STFU.Parallelism.Tests.csproj -c $Configuration -v minimal
dotnet test tests/STFU.Rendering.Abstractions.Tests/STFU.Rendering.Abstractions.Tests.csproj -c $Configuration -v minimal
dotnet test tests/STFU.Rendering.Cpu.Tests/STFU.Rendering.Cpu.Tests.csproj -c $Configuration -v minimal
dotnet test tests/STFU.NPR.Parity.Tests/STFU.NPR.Parity.Tests.csproj -c $Configuration -v minimal

dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-fullcpu 320 240
dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-gpu-present 320 240
dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-gpu-readback 320 240
dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --verify-render-parity default 320 240 3
dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --bench-render-profiles $Asset 800 600 60 npr 10 --animation fixed-step

Write-Host "STFU NPR final optimization validation completed."
