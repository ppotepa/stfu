[CmdletBinding()]
param(
    [int]$MaxReferences = 180
)

$ErrorActionPreference = 'Stop'

function Write-Section([string]$Name) {
    Write-Output ""
    Write-Output "## $Name"
}

Write-Section "solutions"
if (Get-Command rg -ErrorAction SilentlyContinue) {
    rg --files -g '*.sln' -g '*.slnx' -g '*.slnf' | Sort-Object
} else {
    Get-ChildItem -Recurse -File -Include *.sln,*.slnx,*.slnf |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs|\.idea)\\' } |
        Select-Object -ExpandProperty FullName
}

Write-Section "projects"
if (Get-Command rg -ErrorAction SilentlyContinue) {
    rg --files -g '*.csproj' src | Sort-Object
} else {
    Get-ChildItem src -Recurse -File -Include *.csproj |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Select-Object -ExpandProperty FullName
}

Write-Section "project/package references"
if (Get-Command rg -ErrorAction SilentlyContinue) {
    rg 'ProjectReference|PackageReference' src -n --glob '*.csproj' |
        Select-Object -First $MaxReferences
} else {
    Get-ChildItem src -Recurse -File -Include *.csproj |
        Select-String -Pattern 'ProjectReference|PackageReference' |
        Select-Object -First $MaxReferences
}
