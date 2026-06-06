param(
    [string]$Version = '',

    [string]$RuntimeIdentifier = 'win-x64',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Project = 'src/runtime/STFU.App/STFU.App.csproj',

    [string]$OutputRoot = 'release',

    [string]$AssetsPath = 'assets',

    [string]$NativeBuildScript = 'tools/build-native-fbx.ps1',

    [switch]$NoAot,

    [switch]$NoAssets,

    [switch]$SkipNative,

    [switch]$NoRestore,

    [switch]$KeepDiagnostics,

    [switch]$InvariantGlobalization,

    [switch]$Zip,

    [switch]$Force,

    [switch]$DryRun,

    [string[]]$ExtraPublishProperty = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message"
}

function Resolve-FullPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $script:RepoRoot $Path))
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$ArgumentList,
        [string]$WorkingDirectory = $script:RepoRoot
    )

    $commandLine = "$FilePath $($ArgumentList -join ' ')"
    if ($DryRun) {
        Write-Host "[dry-run] $commandLine"
        return
    }

    Write-Host $commandLine
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $commandLine"
    }
}

function Assert-ChildPath {
    param(
        [string]$Path,
        [string]$Parent
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullParent = [System.IO.Path]::GetFullPath($Parent).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $prefix = $fullParent + [System.IO.Path]::DirectorySeparatorChar

    if (-not $fullPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside expected directory. Path='$fullPath', Parent='$fullParent'."
    }
}

function Get-GitValue {
    param([string[]]$Arguments)

    try {
        $value = & git @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) {
            return ($value | Select-Object -First 1)
        }
    } catch {
        return $null
    }

    return $null
}

function Get-DefaultVersion {
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')
    $shortSha = Get-GitValue @('rev-parse', '--short', 'HEAD')
    if ([string]::IsNullOrWhiteSpace($shortSha)) {
        return $stamp
    }

    return "$stamp-$shortSha"
}

function Convert-ToSafePathSegment {
    param([string]$Value)

    $invalid = [System.IO.Path]::GetInvalidFileNameChars()
    $builder = [System.Text.StringBuilder]::new($Value.Length)
    foreach ($ch in $Value.ToCharArray()) {
        if ($invalid -contains $ch) {
            [void]$builder.Append('-')
        } else {
            [void]$builder.Append($ch)
        }
    }

    return $builder.ToString().Trim()
}

function Copy-Assets {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        Write-Warning "Assets path does not exist: $Source"
        return 0
    }

    $sourceRoot = [System.IO.Path]::GetFullPath($Source).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $files = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Force |
        Where-Object {
            $_.Name -ne 'AGENTS.MD' -and
            $_.Name -notlike '*.tmp' -and
            $_.Name -notlike '*.temp' -and
            $_.Name -notlike '*.bak'
        }

    $count = 0
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $target = Join-Path $Destination $relative
        $targetDirectory = Split-Path -Parent $target

        if (-not $DryRun) {
            New-Item -ItemType Directory -Force $targetDirectory | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $target -Force
        }

        $count++
    }

    return $count
}

$script:RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $script:RepoRoot

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-DefaultVersion
}

$safeVersion = Convert-ToSafePathSegment $Version
if ([string]::IsNullOrWhiteSpace($safeVersion)) {
    throw 'Version produced an empty release directory name.'
}

$projectPath = Resolve-FullPath $Project
$outputRootPath = Resolve-FullPath $OutputRoot
$releasePath = Join-Path $outputRootPath $safeVersion
$assetsSourcePath = Resolve-FullPath $AssetsPath
$assetsTargetPath = Join-Path $releasePath 'assets'
$nativeScriptPath = Resolve-FullPath $NativeBuildScript
$publishAot = -not $NoAot

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project was not found: $projectPath"
}

Assert-ChildPath -Path $releasePath -Parent $outputRootPath

Write-Step "Preparing release '$safeVersion'"
Write-Host "repo:       $script:RepoRoot"
Write-Host "project:    $projectPath"
Write-Host "runtime:    $RuntimeIdentifier"
Write-Host "aot:        $publishAot"
Write-Host "output:     $releasePath"

if ((Test-Path -LiteralPath $releasePath) -and -not $Force) {
    throw "Release directory already exists. Pass -Force to replace it: $releasePath"
}

if ((Test-Path -LiteralPath $releasePath) -and $Force) {
    Assert-ChildPath -Path $releasePath -Parent $outputRootPath
    Write-Step "Removing existing release directory"
    if ($DryRun) {
        Write-Host "[dry-run] Remove-Item -Recurse -Force $releasePath"
    } else {
        Remove-Item -LiteralPath $releasePath -Recurse -Force
    }
}

if (-not $DryRun) {
    New-Item -ItemType Directory -Force $releasePath | Out-Null
}

if (-not $SkipNative) {
    if (-not (Test-Path -LiteralPath $nativeScriptPath)) {
        throw "Native build script was not found: $nativeScriptPath"
    }

    Write-Step 'Building native dependencies'
    Invoke-Checked -FilePath 'powershell' -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $nativeScriptPath,
        '-Configuration',
        $Configuration
    )
}

$publishArgs = @(
    'publish',
    $projectPath,
    '-c',
    $Configuration,
    '-r',
    $RuntimeIdentifier,
    '--self-contained',
    'true',
    '-o',
    $releasePath,
    '-v',
    'minimal',
    "-p:PublishAot=$($publishAot.ToString().ToLowerInvariant())",
    '-p:IlcOptimizationPreference=Speed',
    '-p:OptimizationPreference=Speed',
    '-p:DebugType=none',
    '-p:DebugSymbols=false',
    '-p:StripSymbols=true'
)

if ($publishAot) {
    $publishArgs += @(
        '-p:PublishTrimmed=true',
        '-p:TrimMode=full'
    )
}

if (-not $KeepDiagnostics) {
    $publishArgs += @(
        '-p:DebuggerSupport=false',
        '-p:EventSourceSupport=false'
    )
}

if ($InvariantGlobalization) {
    $publishArgs += '-p:InvariantGlobalization=true'
}

if ($NoRestore) {
    $publishArgs += '--no-restore'
}

foreach ($property in $ExtraPublishProperty) {
    if ([string]::IsNullOrWhiteSpace($property)) {
        continue
    }

    $publishArgs += "-p:$property"
}

Write-Step 'Publishing application'
Invoke-Checked -FilePath 'dotnet' -ArgumentList $publishArgs

$assetCount = 0
if (-not $NoAssets) {
    Write-Step 'Copying assets'
    $assetCount = Copy-Assets -Source $assetsSourcePath -Destination $assetsTargetPath
    Write-Host "assets copied: $assetCount"
}

$exeName = if ($RuntimeIdentifier.StartsWith('win-', [System.StringComparison]::OrdinalIgnoreCase)) {
    'STFU.App.exe'
} else {
    'STFU.App'
}

$manifestPath = Join-Path $releasePath 'release.json'
$gitCommit = Get-GitValue @('rev-parse', 'HEAD')
$gitStatus = Get-GitValue @('status', '--short')
$manifest = [ordered]@{
    version = $Version
    directoryName = $safeVersion
    createdUtc = (Get-Date).ToUniversalTime().ToString('O')
    project = $Project
    configuration = $Configuration
    runtimeIdentifier = $RuntimeIdentifier
    publishAot = $publishAot
    selfContained = $true
    keepDiagnostics = [bool]$KeepDiagnostics
    invariantGlobalization = [bool]$InvariantGlobalization
    assetsIncluded = -not $NoAssets
    assetsCopied = $assetCount
    nativeBuildSkipped = [bool]$SkipNative
    executable = $exeName
    gitCommit = $gitCommit
    gitDirty = -not [string]::IsNullOrWhiteSpace($gitStatus)
    publishProperties = $publishArgs | Where-Object { $_ -like '-p:*' }
}

if ($DryRun) {
    Write-Host "[dry-run] Write release manifest: $manifestPath"
} else {
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
}

$runScriptPath = Join-Path $releasePath 'run.ps1'
$runScript = @"
`$ErrorActionPreference = 'Stop'
Set-Location `$PSScriptRoot
& .\$exeName @args
"@

if ($DryRun) {
    Write-Host "[dry-run] Write launcher: $runScriptPath"
} else {
    Set-Content -LiteralPath $runScriptPath -Value $runScript -Encoding UTF8
}

if ($Zip) {
    $zipPath = "$releasePath.zip"
    if ((Test-Path -LiteralPath $zipPath) -and $Force) {
        if ($DryRun) {
            Write-Host "[dry-run] Remove-Item -Force $zipPath"
        } else {
            Remove-Item -LiteralPath $zipPath -Force
        }
    }

    if ((Test-Path -LiteralPath $zipPath) -and -not $Force) {
        throw "Zip already exists. Pass -Force to replace it: $zipPath"
    }

    Write-Step 'Creating zip archive'
    if ($DryRun) {
        Write-Host "[dry-run] Compress-Archive $releasePath $zipPath"
    } else {
        Compress-Archive -LiteralPath $releasePath -DestinationPath $zipPath
    }
}

Write-Step 'Release complete'
Write-Host $releasePath
