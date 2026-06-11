param(
    [string] $Configuration = "Debug",
    [string] $OutputDirectory = "artifacts/interactive-performance/evidence",
    [int] $Frames = 6,
    [int] $Width = 320,
    [int] $Height = 240,
    [switch] $SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Set-Location $repoRoot

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$summaryPath = Join-Path $OutputDirectory "interactive-evidence-$timestamp.txt"
$csvPath = Join-Path $OutputDirectory "interactive-evidence-$timestamp.csv"

function Write-EvidenceLine {
    param([string] $Value)
    $Value | Tee-Object -FilePath $summaryPath -Append
}

Write-EvidenceLine "STFU Interactive Performance evidence export"
Write-EvidenceLine "Repository: $repoRoot"
Write-EvidenceLine "Configuration: $Configuration"
Write-EvidenceLine "Frames: $Frames"
Write-EvidenceLine "Resolution: ${Width}x${Height}"
Write-EvidenceLine "StartedAt: $((Get-Date).ToString('O'))"
Write-EvidenceLine ""

if (-not $SkipBuild) {
    Write-EvidenceLine "[build] dotnet build STFU.slnx -c $Configuration"
    dotnet build STFU.slnx -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
    Write-EvidenceLine ""
}

$rows = @()
$benchScript = Join-Path $repoRoot "tools/ci/run-interactive-performance-parity-gate.ps1"
if (Test-Path -LiteralPath $benchScript) {
    Write-EvidenceLine "[parity] $benchScript"
    & powershell -NoProfile -ExecutionPolicy Bypass -File $benchScript -Configuration $Configuration -Frames $Frames -Width $Width -Height $Height -MinimumSpeedupRatio 0.01
    $rows += "parity,executed,$LASTEXITCODE"
} else {
    Write-EvidenceLine "[parity] skipped: script missing"
    $rows += "parity,missing,0"
}

$analysisScript = Join-Path $repoRoot "tools/ci/run-interactive-performance-analysis-suite.ps1"
if (Test-Path -LiteralPath $analysisScript) {
    Write-EvidenceLine "[analysis] $analysisScript"
    & powershell -NoProfile -ExecutionPolicy Bypass -File $analysisScript -Configuration $Configuration -Frames $Frames -Width $Width -Height $Height -SkipBuild -MinimumSpeedupRatio 0.01
    $rows += "analysis,executed,$LASTEXITCODE"
} else {
    Write-EvidenceLine "[analysis] skipped: script missing"
    $rows += "analysis,missing,0"
}

"kind,status,exitCode" | Set-Content -LiteralPath $csvPath -Encoding UTF8
$rows | Add-Content -LiteralPath $csvPath -Encoding UTF8

Write-EvidenceLine ""
Write-EvidenceLine "summary: $summaryPath"
Write-EvidenceLine "csv: $csvPath"
Write-EvidenceLine "STFU Interactive Performance evidence export completed."
