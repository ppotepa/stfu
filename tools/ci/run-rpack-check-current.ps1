[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$Package,
    [string]$Repo = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Repo)) {
    $Repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

Write-Host "Inspecting package: $Package"
rpack inspect $Package

Write-Host "Linting package: $Package"
rpack lint $Package

Write-Host "Checking package against repo: $Repo"
rpack check $Package $Repo --allow-dirty
