param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [string] $AssetPath = 'assets/walking.fbx',

    [int] $Width = 320,

    [int] $Height = 240,

    [int] $Frames = 12,

    [string] $ReportDirectory = 'logs/interactive-performance-parity',

    [double] $MinimumSpeedupRatio = 1.05,

    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..' '..')
Set-Location $repoRoot

New-Item -ItemType Directory -Force -Path $ReportDirectory | Out-Null
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summaryPath = Join-Path $ReportDirectory "interactive-parity-gate-$timestamp.txt"
$csvPath = Join-Path $ReportDirectory "interactive-parity-gate-$timestamp.csv"

function Write-GateLine {
    param([string] $Value)
    $Value | Tee-Object -FilePath $summaryPath -Append
}

function Invoke-BenchmarkStrategy {
    param(
        [string] $Strategy,
        [string] $LogPath
    )

    if ($Strategy -eq 'interactive') {
        $env:STFU_FRAME_PIPELINE_STRATEGY = 'InteractivePerformance'
        $env:STFU_INTERACTIVE_PREVIEW_OUTPUT = '1'
        $env:STFU_INTERACTIVE_REFERENCE_EXECUTION = 'late-fallback'
        $env:STFU_INTERACTIVE_PREFER_SELF_CONTAINED_PROJECTION = '1'
    } else {
        $env:STFU_FRAME_PIPELINE_STRATEGY = 'ReferenceQuality'
        $env:STFU_INTERACTIVE_PREVIEW_OUTPUT = $null
        $env:STFU_INTERACTIVE_REFERENCE_EXECUTION = $null
        $env:STFU_INTERACTIVE_PREFER_SELF_CONTAINED_PROJECTION = $null
    }

    $arguments = @(
        'run',
        '--project', 'src/runtime/STFU.App/STFU.App.csproj',
        '-c', $Configuration,
        '--',
        '--bench-render-profiles', $AssetPath, $Width, $Height, $Frames, 'default', '1',
        '--animation', 'fixed-step'
    )

    Write-GateLine "[bench] strategy=$Strategy size=${Width}x$Height frames=$Frames"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & dotnet @arguments 2>&1 | Tee-Object -FilePath $LogPath
    $exitCode = $LASTEXITCODE
    $sw.Stop()

    if ($exitCode -ne 0) {
        throw "Benchmark strategy '$Strategy' failed with exit code $exitCode"
    }

    return [Math]::Max(1, [Math]::Round($sw.Elapsed.TotalMilliseconds))
}

Write-GateLine 'STFU Interactive Performance parity gate'
Write-GateLine "Repository: $repoRoot"
Write-GateLine "Configuration: $Configuration"
Write-GateLine "AssetPath: $AssetPath"
Write-GateLine "Resolution: ${Width}x$Height"
Write-GateLine "Frames: $Frames"
Write-GateLine "MinimumSpeedupRatio: $MinimumSpeedupRatio"
Write-GateLine "StartedAt: $((Get-Date).ToString('O'))"
Write-GateLine ''

if (-not $SkipBuild) {
    Write-GateLine "[build] dotnet build STFU.slnx -c $Configuration"
    dotnet build STFU.slnx -c $Configuration 2>&1 | Tee-Object -FilePath $summaryPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
    Write-GateLine ''
}

$referenceLog = Join-Path $ReportDirectory "reference-$timestamp.log"
$interactiveLog = Join-Path $ReportDirectory "interactive-$timestamp.log"
$referenceMs = Invoke-BenchmarkStrategy -Strategy 'reference' -LogPath $referenceLog
$interactiveMs = Invoke-BenchmarkStrategy -Strategy 'interactive' -LogPath $interactiveLog
$speedup = [Math]::Round($referenceMs / [Math]::Max(1, $interactiveMs), 3)

Write-GateLine ''
Write-GateLine "referenceElapsedMs: $referenceMs"
Write-GateLine "interactiveElapsedMs: $interactiveMs"
Write-GateLine "speedupRatio: $speedup"
Write-GateLine "FinishedAt: $((Get-Date).ToString('O'))"

'strategy,width,height,frames,elapsedMs,logFile' | Set-Content -LiteralPath $csvPath
"reference,$Width,$Height,$Frames,$referenceMs,$referenceLog" | Add-Content -LiteralPath $csvPath
"interactive,$Width,$Height,$Frames,$interactiveMs,$interactiveLog" | Add-Content -LiteralPath $csvPath

if ($speedup -lt $MinimumSpeedupRatio) {
    throw "Interactive Performance parity gate failed: speedup $speedup is below required $MinimumSpeedupRatio"
}

Write-GateLine 'Parity gate passed.'
Write-Host "Interactive Performance parity gate summary: $summaryPath"
Write-Host "Interactive Performance parity gate CSV: $csvPath"
