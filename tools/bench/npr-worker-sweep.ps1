param(
    [string]$Configuration = "Release",
    [int]$Width = 640,
    [int]$Height = 360,
    [int[]]$Workers = @(1, 2, 4, 8, 12, 16),
    [switch]$GpuPresent,
    [switch]$GpuReadback,
    [switch]$RangeTimings
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$app = Join-Path $root "src/runtime/STFU.App/STFU.App.csproj"

foreach ($worker in $Workers) {
    $mode = "--smoke-fullcpu"
    if ($GpuPresent) { $mode = "--smoke-gpu-present" }
    if ($GpuReadback) { $mode = "--smoke-gpu-readback" }

    $extraArgs = @($mode, $Width, $Height, "--workers", $worker)
    if ($RangeTimings) {
        $extraArgs += "--npr-range-timings"
    }

    Write-Host "=== workers=$worker mode=$mode size=${Width}x${Height} rangeTimings=$($RangeTimings.IsPresent) ==="
    dotnet run --project $app -c $Configuration -- @extraArgs
}
