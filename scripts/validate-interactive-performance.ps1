param(
    [string] $Configuration = "Debug",
    [switch] $SkipBuild,
    [switch] $RunSmoke,
    [int] $SmokeWidth = 320,
    [int] $SmokeHeight = 240,
    [string] $ReportDirectory = "artifacts/rpack-validation/interactive-performance"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $ReportDirectory "interactive-validation-$timestamp.txt"

function Write-ReportLine {
    param([string] $Value)
    $Value | Tee-Object -FilePath $reportPath -Append
}

Write-ReportLine "STFU Interactive Performance validation"
Write-ReportLine "Repository: $repoRoot"
Write-ReportLine "Configuration: $Configuration"
Write-ReportLine "StartedAt: $((Get-Date).ToString('O'))"
if (-not [string]::IsNullOrWhiteSpace($env:RPACK_PACKAGE_ID)) {
    Write-ReportLine "RPACK_PACKAGE_ID: $env:RPACK_PACKAGE_ID"
    Write-ReportLine "RPACK_APPLY_ID: $env:RPACK_APPLY_ID"
}
if (-not [string]::IsNullOrWhiteSpace($env:RPACK_CHANGED_FILES)) {
    Write-ReportLine "RPACK_CHANGED_FILES:"
    foreach ($changedFile in $env:RPACK_CHANGED_FILES.Split([System.IO.Path]::PathSeparator)) {
        if (-not [string]::IsNullOrWhiteSpace($changedFile)) {
            Write-ReportLine "  - $changedFile"
        }
    }
}
Write-ReportLine ""

$dotnetVersion = dotnet --version
Write-ReportLine "dotnet: $dotnetVersion"
Write-ReportLine "git: $(git rev-parse --short HEAD)"
Write-ReportLine ""

if (-not $SkipBuild) {
    Write-ReportLine "[build] dotnet build STFU.slnx -c $Configuration"
    dotnet build STFU.slnx -c $Configuration 2>&1 | Tee-Object -FilePath $reportPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
    Write-ReportLine ""
}

Write-ReportLine "[test] dotnet test tests/STFU.NPR.Pipelines.Tests/STFU.NPR.Pipelines.Tests.csproj -c $Configuration --filter Interactive"
dotnet test tests/STFU.NPR.Pipelines.Tests/STFU.NPR.Pipelines.Tests.csproj -c $Configuration --filter Interactive 2>&1 | Tee-Object -FilePath $reportPath -Append
if ($LASTEXITCODE -ne 0) {
    throw "Interactive pipeline tests failed with exit code $LASTEXITCODE"
}
Write-ReportLine ""

Write-ReportLine "[contract] checking Interactive Performance source markers"
$requiredMarkers = @(
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveStrokeFrameBuilder.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveStrokeFrameStage.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractivePreviewPolicy.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveOutputHealthAnalyzer.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveReferenceExecutionPolicy.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveCandidateEdgeSource.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveBudgetDecision.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveBudgetPressure.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveFrameBenchmarkReporter.cs",
    "tools/ci/run-interactive-performance-bench.ps1",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/ProjectedTriangleCandidateEdgeBuilder.cs",
    "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ArtifactStore.cs",
    "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
    "src/runtime/STFU.UI.Bridge/Scene/ScenePanelViewModel.cs"
)
foreach ($marker in $requiredMarkers) {
    if (-not (Test-Path -LiteralPath $marker)) {
        throw "Missing expected Interactive Performance source marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
Write-ReportLine ""

Write-ReportLine "[contract] checking build-fix markers"
$selectorText = Get-Content -Raw -LiteralPath "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs"
if ($selectorText -notlike "*using STFU.Rendering.Abstractions.Execution;*") {
    throw "ViewportFramePipelineSelector is missing the NprRenderContentKind namespace import."
}
$scenePanelText = Get-Content -Raw -LiteralPath "src/runtime/STFU.UI.Bridge/Scene/ScenePanelViewModel.cs"
if ($scenePanelText -like "*_suspendEntityCommit*") {
    throw "ScenePanelViewModel still contains the unused _suspendEntityCommit field."
}
$bridgeText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs"
foreach ($counter in @(
    "InteractivePerformance.outputHealthStatus",
    "InteractivePerformance.outputHealthScore",
    "InteractivePerformance.outputHealthWarningCount",
    "InteractivePerformance.previewCandidateReadinessScore",
    "InteractivePerformance.previewRejectedByReadinessGate",
    "InteractivePerformance.previewRejectedBySegmentBudget"
)) {
    if ($bridgeText -notlike "*$counter*") {
        throw "Missing Interactive Performance health counter: $counter"
    }
    Write-ReportLine "  ok $counter"
}
Write-ReportLine ""


Write-ReportLine "[contract] checking preview output gate markers"
$previewPolicyText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractivePreviewPolicy.cs"
foreach ($marker in @(
    "InteractivePreviewMinReadinessScore",
    "OutputReadinessTooLow",
    "StrokeSegmentBudgetExceeded"
)) {
    if ($previewPolicyText -notlike "*$marker*") {
        throw "Missing Interactive Performance preview gate marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
Write-ReportLine ""

Write-ReportLine "[contract] checking reference execution policy markers"
$referencePolicyText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveReferenceExecutionPolicy.cs"
foreach ($marker in @(
    "STFU_INTERACTIVE_REFERENCE_EXECUTION",
    "BeforeInteractive",
    "LateFallback",
    "DisabledForViewportPreview",
    "ReferenceDisabledForPreview"
)) {
    if ($referencePolicyText -notlike "*$marker*") {
        throw "Missing Interactive Performance reference execution marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
$pipelineText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/InteractivePerformanceNprPipeline.cs"
foreach ($marker in @(
    "InteractiveReferenceExecutionPolicy.Resolve",
    "EnsureReferenceFallbackFrame",
    "CaptureReferenceExecution"
)) {
    if ($pipelineText -notlike "*$marker*") {
        throw "Missing Interactive Performance reference execution pipeline marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
Write-ReportLine ""

Write-ReportLine "[contract] checking artifact pruning markers"
$artifactStoreText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ArtifactStore.cs"
foreach ($marker in @(
    "PruneFrameOrCameraArtifacts",
    "PruneFrameOrCameraArtifactsPerKind",
    "PruneTotalFrameOrCameraArtifacts"
)) {
    if ($artifactStoreText -notlike "*$marker*") {
        throw "Missing Interactive Performance artifact pruning marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
$optionsText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.Abstractions/FramePipelineStrategyOptions.cs"
foreach ($option in @(
    "MaxFrameOrCameraArtifactsPerKind",
    "MaxTotalFrameOrCameraArtifacts",
    "EnableReferenceFreeInteractivePreview",
    "MaxInteractiveCandidateEdges",
    "MaxInteractiveStrokeCommands",
    "MaxInteractiveVisibleStrokeSegments",
    "DeferToneCoverageWhenPreviewDoesNotRequireTone"
)) {
    if ($optionsText -notlike "*$option*") {
        throw "Missing Interactive Performance artifact retention option: $option"
    }
    Write-ReportLine "  ok $option"
}
Write-ReportLine ""

Write-ReportLine "[contract] checking self-contained candidate edge markers"
$candidateStageText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/CandidateEdgeStage.cs"
foreach ($marker in @(
    "InteractiveCandidateEdgeSource.ProjectedTriangleEdges",
    "ProjectedTriangleCandidateEdgeBuilder.BuildEdges",
    "CandidateEdgesBuiltFromProjectedTriangles"
)) {
    if ($candidateStageText -notlike "*$marker*") {
        throw "Missing Interactive Performance self-contained candidate marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
$diagnosticsBridgeText = Get-Content -Raw -LiteralPath "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs"
foreach ($counter in @(
    "InteractivePerformance.candidateEdgeSource",
    "InteractivePerformance.candidateEdgesBuiltFromProjectedTriangles",
    "InteractivePerformance.candidateEdgeSourceProjectedTriangles"
)) {
    if ($diagnosticsBridgeText -notlike "*$counter*") {
        throw "Missing Interactive Performance candidate edge counter: $counter"
    }
    Write-ReportLine "  ok $counter"
}
Write-ReportLine ""

Write-ReportLine "[contract] checking interactive budget markers"
foreach ($marker in @(
    "InteractiveBudgetLimiter",
    "CandidateEdgeBudgetApplied",
    "StrokeCommandBudgetApplied",
    "VisibleSegmentBudgetApplied",
    "TonePlanningDeferred",
    "STFU_INTERACTIVE_REFERENCE_FREE_PREVIEW",
    "STFU_INTERACTIVE_MAX_CANDIDATE_EDGES",
    "STFU_INTERACTIVE_MAX_STROKE_COMMANDS",
    "STFU_INTERACTIVE_MAX_VISIBLE_SEGMENTS",
    "ResolveBudgetDecision",
    "OverBudgetStreak",
    "UnderBudgetStreak",
    "EffectiveMaxCandidateEdges",
    "InteractivePerformance.budgetPressure",
    "InteractivePerformance.effectiveToneDeferred"
)) {
    $found = (Get-ChildItem -Path src -Recurse -File -Include *.cs | Select-String -SimpleMatch $marker -Quiet)
    if (-not $found) {
        throw "Missing Interactive Performance budget/reference-free marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
Write-ReportLine ""


Write-ReportLine "[contract] checking benchmark/report markers"
foreach ($marker in @(
    "InteractiveFrameBenchmarkReporter",
    "InteractiveFrameBenchmarkReport",
    "InteractiveFrameBenchmarkSample",
    "TotalInteractiveStageMs",
    "InteractivePerformance.totalInteractiveStageMs",
    "run-interactive-performance-bench.ps1",
    "STFU_FRAME_PIPELINE_STRATEGY",
    "STFU_INTERACTIVE_REFERENCE_EXECUTION"
)) {
    $found = (Get-ChildItem -Path src,tools,scripts,tests -Recurse -File -Include *.cs,*.ps1 | Select-String -SimpleMatch $marker -Quiet)
    if (-not $found) {
        throw "Missing Interactive Performance benchmark/report marker: $marker"
    }
    Write-ReportLine "  ok $marker"
}
Write-ReportLine ""

if ($RunSmoke) {
    Write-ReportLine "[smoke] STFU.App --smoke-gpu-present $SmokeWidth $SmokeHeight"
    dotnet run --project src/runtime/STFU.App/STFU.App.csproj -c $Configuration -- --smoke-gpu-present $SmokeWidth $SmokeHeight 2>&1 | Tee-Object -FilePath $reportPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "Interactive smoke failed with exit code $LASTEXITCODE"
    }
    Write-ReportLine ""
}

Write-ReportLine "FinishedAt: $((Get-Date).ToString('O'))"
Write-ReportLine "Validation passed."
Write-Host "Interactive Performance validation report: $reportPath"
