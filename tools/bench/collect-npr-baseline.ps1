param(
    [string]$Configuration = "Release",
    [int]$Width = 640,
    [int]$Height = 360
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$app = Join-Path $root "src/runtime/STFU.App/STFU.App.csproj"
$workers = @(1, 2, 4, 8, 12, 16)
$modes = @("--smoke-fullcpu", "--smoke-gpu-present", "--smoke-gpu-readback")

foreach ($mode in $modes) {
    foreach ($worker in $workers) {
        $env:STFU_NPR_WORKERS = "$worker"
        Write-Host "mode=$mode workers=$worker"
        dotnet run --project $app -c $Configuration -- $mode $Width $Height --workers $worker
    }
}

Remove-Item Env:STFU_NPR_WORKERS -ErrorAction SilentlyContinue
