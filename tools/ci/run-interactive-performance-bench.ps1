param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [string] $AssetPath = 'assets/walking.fbx',

    [int[]] $Widths = @(320, 640),

    [int[]] $Heights = @(240, 480),

    [int] $Frames = 12,

    [string] $ReportDirectory = 'artifacts/interactive-performance-bench',

    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..' '..')
Set-Location $repoRoot

New-Item -ItemType Directory -Force -Path $ReportDirectory | Out-Null
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summaryPath = Join-Path $ReportDirectory "interactive-performance-bench-$timestamp.txt"
$csvPath = Join-Path $ReportDirectory "interactive-performance-bench-$timestamp.csv"

function Write-BenchLine {
    param([string] $Value)
    $Value | Tee-Object -FilePath $summaryPath -Append
}

function Write-CsvLine {
    param([string] $Value)
    $Value | Add-Content -LiteralPath $csvPath
}

Write-BenchLine 'STFU Interactive Performance benchmark sweep'
Write-BenchLine "Repository: $repoRoot"
Write-BenchLine "Configuration: $Configuration"
Write-BenchLine "AssetPath: $AssetPath"
Write-BenchLine "Frames: $Frames"
Write-BenchLine "StartedAt: $((Get-Date).ToString('O'))"
Write-BenchLine ''
Write-CsvLine 'strategy,width,height,frames,exitCode,elapsedMs,logFile'

if (-not $SkipBuild) {
    Write-BenchLine "[build] dotnet build STFU.slnx -c $Configuration"
    dotnet build STFU.slnx -c $Configuration 2>&1 | Tee-Object -FilePath $summaryPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
    Write-BenchLine ''
}

$strategies = @('reference', 'interactive')
for ($index = 0; $index -lt $Widths.Count; $index++) {
    $width = $Widths[$index]
    $height = if ($index -lt $Heights.Count) { $Heights[$index] } else { $Heights[$Heights.Count - 1] }

    foreach ($strategy in $strategies) {
        $logPath = Join-Path $ReportDirectory "bench-$strategy-${width}x$height-$timestamp.log"
        $arguments = @(
            'run',
            '--project', 'src/runtime/STFU.App/STFU.App.csproj',
            '-c', $Configuration,
            '--',
            '--bench-render-profiles', $AssetPath, $width, $height, $Frames, 'default', '1',
            '--animation', 'fixed-step'
        )

        if ($strategy -eq 'interactive') {
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

        Write-BenchLine "[bench] strategy=$strategy size=${width}x$height frames=$Frames"
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        & dotnet @arguments 2>&1 | Tee-Object -FilePath $logPath
        $exitCode = $LASTEXITCODE
        $sw.Stop()
        Write-BenchLine "  exitCode=$exitCode elapsedMs=$([Math]::Round($sw.Elapsed.TotalMilliseconds)) log=$logPath"
        Write-CsvLine "$strategy,$width,$height,$Frames,$exitCode,$([Math]::Round($sw.Elapsed.TotalMilliseconds)),$logPath"

        if ($exitCode -ne 0) {
            throw "Benchmark failed for strategy '$strategy' ${width}x$height with exit code $exitCode"
        }
    }
}

$env:STFU_FRAME_PIPELINE_STRATEGY = $null
$env:STFU_INTERACTIVE_PREVIEW_OUTPUT = $null
$env:STFU_INTERACTIVE_REFERENCE_EXECUTION = $null
$env:STFU_INTERACTIVE_PREFER_SELF_CONTAINED_PROJECTION = $null

Write-BenchLine ''
Write-BenchLine "FinishedAt: $((Get-Date).ToString('O'))"
Write-BenchLine "CSV: $csvPath"
Write-Host "Interactive Performance benchmark summary: $summaryPath"
Write-Host "Interactive Performance benchmark CSV: $csvPath"
