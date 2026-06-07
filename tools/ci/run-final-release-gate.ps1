[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [switch] $SkipBenchmarks
)

& (Join-Path $PSScriptRoot "run-npr-final-gate.ps1") -Configuration $Configuration -SkipBenchmarks:$SkipBenchmarks
