$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sourceRoots = @(
    "src/aot",
    "src/runtime/STFU.Rendering.DirectX",
    "src/runtime/STFU.UI.Bridge"
)

$pattern = '(?<!Deterministic)Parallel\.(For|ForEach|Invoke)'
$taskRunPattern = 'Task\.Run\s*\('
$threadAuditPattern = 'new\s+Thread\s*\('

$allowed = @(
    'src/aot/STFU.Parallelism/DeterministicParallel.cs',
    'src/aot/STFU.Rendering.Abstractions/Execution/LatestNprRenderScheduler.cs'
)

$violations = @()
foreach ($rootRel in $sourceRoots) {
    $dir = Join-Path $root $rootRel
    if (!(Test-Path $dir)) { continue }
    $files = Get-ChildItem $dir -Recurse -Filter *.cs
    foreach ($file in $files) {
        $rel = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
        if ($allowed -contains $rel) { continue }
        $text = Get-Content $file.FullName -Raw
        if ($text -match $pattern -or $text -match $taskRunPattern -or $text -match $threadAuditPattern) {
            $violations += $rel
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Error ("Forbidden raw parallel/threading usage:`n" + ($violations -join "`n"))
}

Write-Host "Parallelism guard passed."
