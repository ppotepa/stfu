[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string[]]$Assets = @("assets\suzanne.obj", "assets\walking.fbx"),
    [int[]]$Widths = @(320, 800, 1280, 1920),
    [int[]]$Heights = @(240, 600, 720, 1080),
    [int]$Frames = 30,
    [int]$WarmupFrames = 3,
    [string[]]$Modes = @("default", "mesh", "npr"),
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

for ($resolutionIndex = 0; $resolutionIndex -lt $Widths.Count; $resolutionIndex++) {
    $width = $Widths[$resolutionIndex]
    $height = $Heights[$resolutionIndex]
    foreach ($asset in $Assets) {
        foreach ($mode in $Modes) {
            foreach ($worker in $Workers) {
                foreach ($tileSize in $TileSizes) {
                    Write-Host "Render sweep: asset=$asset mode=$mode ${width}x$height workers=$worker tileSize=$tileSize"
                    & dotnet run --project (Join-Path $root "src\runtime\STFU.App\STFU.App.csproj") -c $Configuration -- `
                        --bench-render-profiles $asset $width $height $Frames $mode $WarmupFrames `
                        --workers $worker `
                        --tile-size $tileSize `
                        --worker-budget-mode benchmark `
                        --render-optimizer auto `
                        --animation off

                    $exitCode = $LASTEXITCODE
                    $rows.Add("$asset,$width,$height,$Frames,$WarmupFrames,$mode,$worker,$tileSize,$exitCode")
                    if ($exitCode -ne 0) {
                        $rows | Set-Content -Path $outputPath -Encoding UTF8
                        throw "Render sweep failed for asset=$asset mode=$mode workers=$worker tileSize=$tileSize."
                    }
                }
            }
        }
    }
}

$rows | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "Render sweep completed: $outputPath"
