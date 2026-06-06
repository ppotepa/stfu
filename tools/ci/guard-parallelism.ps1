[CmdletBinding()]
param(
    [string]$Root = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$pattern = '(?<!Deterministic)Parallel\.(For|ForEach|Invoke)'
$paths = @(
    (Join-Path $Root "src\aot"),
    (Join-Path $Root "src\runtime")
)

$scanMatches = & rg -P $pattern @paths -g "*.cs" -n
if ($LASTEXITCODE -gt 1) {
    exit $LASTEXITCODE
}

$unexpected = @(
    $scanMatches | Where-Object {
        $_ -and ($_ -notmatch 'src[/\\]aot[/\\]STFU\.Parallelism[/\\]DeterministicParallel\.cs:')
    }
)

if ($unexpected.Count -gt 0) {
    Write-Error ("Unexpected direct Parallel.* usage outside STFU.Parallelism:`n" + ($unexpected -join "`n"))
    exit 1
}

$scanMatches

$threadAuditPattern = '\b(new\s+Thread|Task\.Run|ThreadPool\.QueueUserWorkItem)\b'
$threadAudit = & rg -P $threadAuditPattern @paths -g "*.cs" -n
if ($LASTEXITCODE -gt 1) {
    exit $LASTEXITCODE
}

if (-not [string]::IsNullOrWhiteSpace($threadAudit)) {
    Write-Host ""
    Write-Host "Thread/task audit:"
    $threadAudit
}

exit 0
