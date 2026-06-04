[CmdletBinding()]
param(
    [string[]]$Path = @(),
    [int]$MaxLines = 220
)

$ErrorActionPreference = 'Stop'

Write-Output "## git status"
git status --short

Write-Output ""
Write-Output "## diff stat"
git diff --stat 2>$null

Write-Output ""
Write-Output "## changed files"
git diff --name-only 2>$null | Select-Object -First 120

if ($Path.Count -gt 0) {
    Write-Output ""
    Write-Output "## focused diff"
    git diff -- $Path 2>$null | Select-Object -First $MaxLines
}
