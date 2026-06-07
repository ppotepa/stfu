param(
    [string] $Configuration = "Release",
    [switch] $SkipBenchmarks
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot
try {
    Write-Host "[final-gate] repository: $repoRoot"
    Write-Host "[final-gate] configuration: $Configuration"

    dotnet build STFU.slnx -c $Configuration

    $testProjects = @(
        "tests/STFU.Rendering.Abstractions.Tests/STFU.Rendering.Abstractions.Tests.csproj",
        "tests/STFU.Rendering.Cpu.Tests/STFU.Rendering.Cpu.Tests.csproj",
        "tests/STFU.NPR.Parity.Tests/STFU.NPR.Parity.Tests.csproj",
        "tests/STFU.NPR.Pipelines.Tests/STFU.NPR.Pipelines.Tests.csproj",
        "tests/STFU.Rendering.DirectX.Tests/STFU.Rendering.DirectX.Tests.csproj"
    )

    foreach ($project in $testProjects) {
        if (Test-Path $project) {
            Write-Host "[final-gate] test $project"
            dotnet test $project -c $Configuration --no-build -v minimal
        }
    }

    powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/run-npr-hotpath-audit.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/guard-parallelism.ps1

    if (-not $SkipBenchmarks) {
        powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/run-npr-final-optimization-validation.ps1 -Configuration $Configuration
    }

    Write-Host "[final-gate] complete"
}
finally {
    Pop-Location
}
