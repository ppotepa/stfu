[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Asset = "assets\suzanne.obj",
    [int]$Width = 800,
    [int]$Height = 600,
    [int]$Frames = 30,
    [int]$WarmupFrames = 3,
    [string]$Mode = "default",
    [int[]]$Workers = @(1, 2, 4, 8, 12, 16, 24),
    [int[]]$TileSizes = @(16, 32, 64, 128),
    [string]$Output = "artifacts\render-sweep.csv"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$outputPath = Join-Path $root $Output
$outputDir = Split-Path -Parent $outputPath
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

$rows = New-Object System.Collections.Generic.List[string]
$rows.Add("asset,width,height,frames,warmupFrames,mode,workers,tileSize,exitCode")

foreach ($worker in $Workers) {
    foreach ($tileSize in $TileSizes) {
        Write-Host "Render sweep: workers=$worker tileSize=$tileSize asset=$Asset"
        & dotnet run --project (Join-Path $root "src\runtime\STFU.App\STFU.App.csproj") -c $Configuration -- `
            --bench-render-profiles $Asset $Width $Height $Frames $Mode $WarmupFrames `
            --workers $worker `
            --tile-size $tileSize `
            --worker-budget-mode benchmark `
            --render-optimizer auto `
            --animation off

        $exitCode = $LASTEXITCODE
        $rows.Add("$Asset,$Width,$Height,$Frames,$WarmupFrames,$Mode,$worker,$tileSize,$exitCode")
        if ($exitCode -ne 0) {
            $rows | Set-Content -Path $outputPath -Encoding UTF8
            throw "Render sweep failed for workers=$worker tileSize=$tileSize."
        }
    }
}

$rows | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "Render sweep completed: $outputPath"
