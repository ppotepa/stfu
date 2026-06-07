param(
    [string]$Project = "src/runtime/STFU.App/STFU.App.csproj",
    [string]$Solution = "STFU.slnx",
    [string]$DefaultAsset = "assets\suzanne.obj",
    [string]$FbxAsset = "assets\walking.fbx",
    [int]$Width = 800,
    [int]$Height = 600,
    [int]$Frames = 60,
    [int]$Warmup = 5,
    [switch]$FullSweep
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "== $Name =="
    & $Command
}

Invoke-Step "Build release solution" {
    dotnet build $Solution -c Release
}

Invoke-Step "Run all tests" {
    dotnet test $Solution -c Release --no-build
}

Invoke-Step "Smoke full CPU" {
    dotnet run --project $Project -c Release --no-build -- --smoke-fullcpu 320 240
}

Invoke-Step "Smoke GPU present" {
    dotnet run --project $Project -c Release --no-build -- --smoke-gpu-present 320 240
}

Invoke-Step "Smoke GPU readback" {
    dotnet run --project $Project -c Release --no-build -- --smoke-gpu-readback 320 240
}

Invoke-Step "Smoke GPU readback with GPU visibility" {
    dotnet run --project $Project -c Release --no-build -- --smoke-gpu-readback 320 240 --gpu-visibility
}

Invoke-Step "Verify NPR parity" {
    dotnet run --project $Project -c Release --no-build -- --verify-render-parity default 320 240 3
}

Invoke-Step "Smoke FBX ABI" {
    dotnet run --project $Project -c Release --no-build -- --smoke-fbx-abi $FbxAsset
}

Invoke-Step "Smoke FBX UI load" {
    dotnet run --project $Project -c Release --no-build -- --smoke-fbx-ui-load $FbxAsset
}

Invoke-Step "Baseline render profile" {
    dotnet run --project $Project -c Release --no-build -- --bench-render-profiles $DefaultAsset $Width $Height $Frames default $Warmup --workers 1 --animation off
}

Invoke-Step "Parallel render profile" {
    dotnet run --project $Project -c Release --no-build -- --bench-render-profiles $DefaultAsset $Width $Height $Frames default $Warmup --workers 16 --animation off
}

if ($FullSweep) {
    $assets = @("assets\suzanne.obj", "assets\walking.fbx", "assets\Goku.obj")
    $resolutions = @(
        @{ W = 320; H = 240; Frames = 60; Warmup = 5 },
        @{ W = 800; H = 600; Frames = 60; Warmup = 5 },
        @{ W = 1280; H = 720; Frames = 45; Warmup = 5 },
        @{ W = 1920; H = 1080; Frames = 30; Warmup = 3 }
    )
    $workers = @(1, 8, 16)
    $tileSizes = @(16, 32, 64)

    foreach ($asset in $assets) {
        foreach ($resolution in $resolutions) {
            foreach ($worker in $workers) {
                Invoke-Step "Sweep asset=$asset size=$($resolution.W)x$($resolution.H) workers=$worker" {
                    dotnet run --project $Project -c Release --no-build -- --bench-render-profiles $asset $resolution.W $resolution.H $resolution.Frames default $resolution.Warmup --workers $worker --animation off
                }
            }

            foreach ($tileSize in $tileSizes) {
                Invoke-Step "Tile sweep asset=$asset size=$($resolution.W)x$($resolution.H) tile=$tileSize" {
                    dotnet run --project $Project -c Release --no-build -- --bench-render-profiles $asset $resolution.W $resolution.H $resolution.Frames default $resolution.Warmup --tile-size $tileSize --animation off
                }
            }
        }
    }
}

Write-Host ""
Write-Host "Final NPR optimization validation completed."
