[CmdletBinding()]
param(
    [string]$Root = "",
    [string]$Output = "artifacts\npr-hotpath-audit.txt"
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$hotPaths = @(
    "src\aot\npr\pipelines\STFU.NPR.Pipelines\Default\Steps",
    "src\aot\STFU.Rendering.Cpu\Rasterization",
    "src\runtime\STFU.Rendering.DirectX\Passes",
    "src\runtime\STFU.Rendering.DirectX\Upload"
)

$patterns = @(
    "\.ToArray\(",
    "\.ToList\(",
    "Enumerable\.",
    "\.Select\(",
    "\.Where\(",
    "\.GroupBy\(",
    "\.OrderBy\(",
    "new List<",
    "new Dictionary<",
    "Parallel\.For",
    "Parallel\.ForEach",
    "Task\.Run",
    "new Thread",
    "ThreadPool\.QueueUserWorkItem"
)

$outputPath = Join-Path $Root $Output
$outputDir = Split-Path -Parent $outputPath
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

$rows = New-Object System.Collections.Generic.List[string]
$rows.Add("STFU NPR hot path audit")
$rows.Add("Root: $Root")
$rows.Add("")

foreach ($relativePath in $hotPaths) {
    $path = Join-Path $Root $relativePath
    if (-not (Test-Path $path)) {
        continue
    }

    Get-ChildItem -Path $path -Filter *.cs -Recurse | ForEach-Object {
        $file = $_.FullName
        $relativeFile = Resolve-Path -Path $file -Relative
        $lineNumber = 0
        Get-Content -Path $file | ForEach-Object {
            $lineNumber++
            $line = $_
            foreach ($pattern in $patterns) {
                if ($line -match $pattern) {
                    $rows.Add(($relativeFile + ":" + $lineNumber + ":" + $pattern + ":" + $line))
                }
            }
        }
    }
}

$rows | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "Hot path audit completed: $outputPath"
