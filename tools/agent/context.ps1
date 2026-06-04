[CmdletBinding()]
param(
    [int]$MaxFiles = 120
)

$ErrorActionPreference = 'Stop'

function Write-Section([string]$Name) {
    Write-Output ""
    Write-Output "## $Name"
}

Write-Section "git status"
git status --short

Write-Section "diff stat"
git diff --stat 2>$null

Write-Section "solutions"
if (Get-Command rg -ErrorAction SilentlyContinue) {
    rg --files -g '*.sln' -g '*.slnx' -g '*.slnf' | Sort-Object | Select-Object -First 20
} else {
    Get-ChildItem -Recurse -File -Include *.sln,*.slnx,*.slnf |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs|\.idea)\\' } |
        Select-Object -First 20 -ExpandProperty FullName
}

Write-Section "projects"
if (Get-Command rg -ErrorAction SilentlyContinue) {
    rg --files -g '*.csproj' src | Sort-Object | Select-Object -First 80
} else {
    Get-ChildItem src -Recurse -File -Include *.csproj |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Select-Object -First 80 -ExpandProperty FullName
}

Write-Section "focused files"
if (Get-Command rg -ErrorAction SilentlyContinue) {
    rg --files src docs maquettes `
        --glob '!**/bin/**' `
        --glob '!**/obj/**' `
        --glob '!third_party/**' `
        --glob '!docs/NPR_SUPPLEMENT_IMPL.md' `
        --glob '!docs/NPR_SUPPLEMENT.md' `
        --glob '!docs/NPR_THEORY.MD' |
        Where-Object { $_ -match 'NPR|Pipeline|Preset|Viewport|UI|Import|Engine|Camera|Mesh|AGENTS|BRIEF|README' } |
        Select-Object -First $MaxFiles
} else {
    Get-ChildItem src,docs,maquettes -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|third_party)\\' } |
        Where-Object { $_.Name -match 'NPR|Pipeline|Preset|Viewport|UI|Import|Engine|Camera|Mesh|AGENTS|BRIEF|README' } |
        Select-Object -First $MaxFiles -ExpandProperty FullName
}
