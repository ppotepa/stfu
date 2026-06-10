param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [int] $Frames = 6,
    [int] $Width = 320,
    [int] $Height = 240,
    [double] $MinimumSpeedupRatio = 0.01,
    [string] $AssetPath = 'assets\walking.fbx',
    [string] $ReportDirectory = 'artifacts/interactive-performance-analysis',
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $repoRoot

New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summaryPath = Join-Path $ReportDirectory "interactive-analysis-$timestamp.txt"

function Write-AnalysisLine {
    param([string] $Value)
    $Value | Tee-Object -FilePath $summaryPath -Append
}

function Invoke-TimedRun {
    param(
        [string] $Strategy,
        [string] $ExtraEnv
    )

    $previousStrategy = $env:STFU_FRAME_PIPELINE_STRATEGY
    $previousPreview = $env:STFU_INTERACTIVE_PREVIEW_OUTPUT
    $previousReference = $env:STFU_INTERACTIVE_REFERENCE_EXECUTION
    try {
        $env:STFU_FRAME_PIPELINE_STRATEGY = $Strategy
        if ($Strategy -eq 'InteractivePerformance') {
            $env:STFU_INTERACTIVE_PREVIEW_OUTPUT = '1'
            if (-not [string]::IsNullOrWhiteSpace($ExtraEnv)) {
                $env:STFU_INTERACTIVE_REFERENCE_EXECUTION = $ExtraEnv
            }
        }

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --bench-render-profiles $AssetPath $Width $Height $Frames default 1 --animation fixed-step | Tee-Object -FilePath $summaryPath -Append
        if ($LASTEXITCODE -ne 0) {
            throw "Benchmark run failed for $Strategy with exit code $LASTEXITCODE"
        }
        $sw.Stop()
        return $sw.Elapsed.TotalMilliseconds
    }
    finally {
        $env:STFU_FRAME_PIPELINE_STRATEGY = $previousStrategy
        $env:STFU_INTERACTIVE_PREVIEW_OUTPUT = $previousPreview
        $env:STFU_INTERACTIVE_REFERENCE_EXECUTION = $previousReference
    }
}

Write-AnalysisLine 'STFU Interactive Performance analysis suite'
Write-AnalysisLine "Repository: $repoRoot"
Write-AnalysisLine "Configuration: $Configuration"
Write-AnalysisLine "Asset: $AssetPath"
Write-AnalysisLine "Resolution: ${Width}x${Height}"
Write-AnalysisLine "Frames: $Frames"
Write-AnalysisLine ''

if (-not $SkipBuild) {
    Write-AnalysisLine '[build] dotnet build STFU.slnx'
    dotnet build STFU.slnx -c $Configuration | Tee-Object -FilePath $summaryPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-AnalysisLine ''
}

Write-AnalysisLine '[run] ReferenceQuality'
$referenceMs = Invoke-TimedRun -Strategy 'ReferenceQuality' -ExtraEnv ''
Write-AnalysisLine "ReferenceQuality elapsed ms: $referenceMs"
Write-AnalysisLine ''

Write-AnalysisLine '[run] InteractivePerformance'
$interactiveMs = Invoke-TimedRun -Strategy 'InteractivePerformance' -ExtraEnv 'reference-free'
Write-AnalysisLine "InteractivePerformance elapsed ms: $interactiveMs"
Write-AnalysisLine ''

$speedup = if ($interactiveMs -le 0) { 0 } else { $referenceMs / $interactiveMs }
Write-AnalysisLine "speedupRatio: $speedup"
Write-AnalysisLine "minimumSpeedupRatio: $MinimumSpeedupRatio"

if ($speedup -lt $MinimumSpeedupRatio) {
    throw "Interactive Performance speedup ratio $speedup is below required minimum $MinimumSpeedupRatio"
}

Write-AnalysisLine 'analysis suite passed'
Write-Host $summaryPath
