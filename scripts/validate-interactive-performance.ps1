param(
    [string] $Configuration = "Debug",
    [switch] $SkipBuild,
    [switch] $RunSmoke,
    [int] $SmokeWidth = 320,
    [int] $SmokeHeight = 240,
    [string] $ReportDirectory = "artifacts/rpack-validation/interactive-performance"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $ReportDirectory "interactive-validation-$timestamp.txt"

function Write-ReportLine {
    param([string] $Value)
    $Value | Tee-Object -FilePath $reportPath -Append
}

Write-ReportLine "STFU Interactive Performance validation"
Write-ReportLine "Repository: $repoRoot"
Write-ReportLine "Configuration: $Configuration"
Write-ReportLine "StartedAt: $((Get-Date).ToString('O'))"
Write-ReportLine ""

$dotnetVersion = dotnet --version
Write-ReportLine "dotnet: $dotnetVersion"
Write-ReportLine "git: $(git rev-parse --short HEAD)"
Write-ReportLine ""

if (-not $SkipBuild) {
    Write-ReportLine "[build] dotnet build STFU.slnx -c $Configuration"
    dotnet build STFU.slnx -c $Configuration 2>&1 | Tee-Object -FilePath $reportPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
    Write-ReportLine ""
}

Write-ReportLine "[test] dotnet test tests/STFU.NPR.Pipelines.Tests/STFU.NPR.Pipelines.Tests.csproj -c $Configuration --filter Interactive"
dotnet test tests/STFU.NPR.Pipelines.Tests/STFU.NPR.Pipelines.Tests.csproj -c $Configuration --filter Interactive 2>&1 | Tee-Object -FilePath $reportPath -Append
if ($LASTEXITCODE -ne 0) {
    throw "Interactive pipeline tests failed with exit code $LASTEXITCODE"
}
Write-ReportLine ""

if ($RunSmoke) {
    Write-ReportLine "[smoke] STFU.App --smoke-gpu-present $SmokeWidth $SmokeHeight"
    dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-gpu-present $SmokeWidth $SmokeHeight 2>&1 | Tee-Object -FilePath $reportPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "Interactive smoke failed with exit code $LASTEXITCODE"
    }
    Write-ReportLine ""
}

Write-ReportLine "FinishedAt: $((Get-Date).ToString('O'))"
Write-ReportLine "Validation passed."
Write-Host "Interactive Performance validation report: $reportPath"
