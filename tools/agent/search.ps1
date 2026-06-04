[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Pattern,

    [string[]]$Path = @('src', 'docs', 'maquettes', 'AGENTS.MD', 'README.md'),

    [int]$MaxLines = 120,

    [switch]$Literal
)

$ErrorActionPreference = 'Stop'

if (Get-Command rg -ErrorAction SilentlyContinue) {
    $args = @()
    if ($Literal) {
        $args += '--fixed-strings'
    }

    $args += @(
        $Pattern
    )

    $args += $Path

    $args += @(
        '-n',
        '-C', '2',
        '--glob', '!**/bin/**',
        '--glob', '!**/obj/**',
        '--glob', '!assets/**',
        '--glob', '!third_party/**',
        '--glob', '!concat.txt',
        '--glob', '!concat.zip'
    )

    & rg @args | Select-Object -First $MaxLines
    if ($LASTEXITCODE -gt 1) {
        throw "rg failed with exit code $LASTEXITCODE."
    }

    return
}

Get-ChildItem $Path -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|assets|third_party)\\' } |
    Select-String -Pattern $Pattern -Context 2,2 |
    Select-Object -First $MaxLines
