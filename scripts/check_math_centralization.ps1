$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$srcRoot = Join-Path $repoRoot "src"

$files = Get-ChildItem -Path $srcRoot -Recurse -File -Filter "*.cs" |
    Where-Object {
        $path = $_.FullName.Replace('\', '/')
        -not $path.Contains('/src/aot/STFU.Common/Math/') -and
        -not $path.Contains('/src/native/') -and
        -not $path.Contains('/vendor/') -and
        -not $path.Contains('/bin/') -and
        -not $path.Contains('/obj/')
    }

$violations = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace('\', '/')
    $content = Get-Content -Raw -Path $file.FullName

    if ($content -match '\b(class|struct)\s+\w*Math\b') {
        $violations.Add("$relative contains local *Math type")
    }

    foreach ($pattern in @(
        'DefaultPathMath',
        'DefaultNoise',
        'private\s+static\s+.*DegreesToRadians',
        'private\s+static\s+.*SignedArea',
        'private\s+static\s+.*PerpendicularDistanceSquared',
        'private\s+static\s+.*TriangleOutsideClip',
        'private\s+static\s+.*NdcOutsideSameSide',
        'private\s+static\s+.*FirstChannelName'
    )) {
        if ($content -match $pattern) {
            $violations.Add("$relative matches forbidden pattern: $pattern")
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Math centralization violations:"
    foreach ($violation in $violations) {
        Write-Host " - $violation"
    }
    exit 1
}

Write-Host "Math centralization check passed."
