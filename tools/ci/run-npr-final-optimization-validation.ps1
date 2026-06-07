[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Asset = "assets\walking.fbx",
    [switch]$SkipGpu,
    [switch]$RunSweep,
    [string]$SweepAsset = "assets\suzanne.obj"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "== $Name =="
    & $Action
}

Write-Host "STFU NPR final optimization validation"
Write-Host "Root: $root"
Write-Host "Configuration: $Configuration"
Write-Host "Asset: $Asset"

Push-Location $root
try {
    Invoke-Step "Build solution" {
        dotnet build STFU.slnx -c $Configuration
    }

    Invoke-Step "Guard parallelism" {
        powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/guard-parallelism.ps1 -Root $root
    }

    Invoke-Step "Parallelism tests" {
        dotnet test tests/STFU.Parallelism.Tests/STFU.Parallelism.Tests.csproj -c $Configuration -v minimal
    }

    Invoke-Step "Rendering abstractions tests" {
        dotnet test tests/STFU.Rendering.Abstractions.Tests/STFU.Rendering.Abstractions.Tests.csproj -c $Configuration -v minimal
    }

    Invoke-Step "CPU renderer tests" {
        dotnet test tests/STFU.Rendering.Cpu.Tests/STFU.Rendering.Cpu.Tests.csproj -c $Configuration -v minimal
    }

    Invoke-Step "NPR parity tests" {
        dotnet test tests/STFU.NPR.Parity.Tests/STFU.NPR.Parity.Tests.csproj -c $Configuration -v minimal
    }

    Invoke-Step "Full CPU smoke" {
        dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-fullcpu 320 240
    }

    if (-not $SkipGpu) {
        Invoke-Step "GPU present smoke" {
            dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-gpu-present 320 240
        }

        Invoke-Step "GPU readback smoke" {
            dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-gpu-readback 320 240
        }

        Invoke-Step "GPU visibility readback smoke" {
            dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-gpu-readback 320 240 --gpu-visibility
        }
    }

    Invoke-Step "Default parity" {
        dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --verify-render-parity default 320 240 3
    }

    if (-not $SkipGpu) {
        Invoke-Step "GPU visibility parity" {
            dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --verify-render-parity default 320 240 3 --gpu-visibility
        }
    }

    Invoke-Step "FBX UI load smoke" {
        dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-fbx-ui-load $Asset
    }

    Invoke-Step "NPR benchmark sample" {
        dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --bench-render-profiles $Asset 800 600 60 npr 10 --animation fixed-step
    }

    if ($RunSweep) {
        Invoke-Step "Worker/tile render sweep" {
            powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/run-render-sweep.ps1 -Configuration $Configuration -Assets @($SweepAsset, $Asset)
        }
    }
}
finally {
    Pop-Location
}

Write-Host "STFU NPR final optimization validation completed."
