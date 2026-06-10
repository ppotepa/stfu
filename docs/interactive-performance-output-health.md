# Interactive Performance output health

This document describes the small diagnostic layer added around the Interactive Performance output contract.

## Goal

Interactive Performance now has enough internal artifacts to describe how close the realtime path is to producing its own viewport frame, but the safe default still returns the Reference Quality frame. The output health report makes that state explicit without silently changing rendering behavior.

## Runtime contract

Reference Quality remains the baseline for final output unless Interactive Performance preview output is explicitly enabled. The health report does not flip that default. It only records whether the Interactive Performance path has projection artifacts, visible geometry, stroke commands, visible stroke segments, a renderable stroke frame, or a complete preview candidate.

## Health statuses

`NoInteractiveArtifacts` means the orchestrator did not produce useful IP artifacts for the frame.

`ProjectionOnly` means projected geometry exists, but no visible geometry or stroke data is ready.

`VisibleGeometry` means visibility or candidate edges are available. This is useful for measuring early culling progress.

`StrokeDataReady` means stroke commands or visible stroke segments are ready, but the final assembled interactive frame is not yet being returned.

`PreviewCandidateReady` means the Interactive Performance path produced a renderable frame candidate.

`ReturningReferenceFallback` means Reference Quality is still the final output. This is expected by default and is not an error.

`ReturningInteractivePreview` means the opt-in preview output path returned the interactive frame.

## Counters

The diagnostics bridge writes numeric counters:

- `InteractivePerformance.outputHealthStatus`
- `InteractivePerformance.outputHealthScore`
- `InteractivePerformance.outputHealthWarningCount`

These counters are intentionally numeric because the existing `NprContext.Counters` channel is numeric. Text is kept in `InteractiveFrameDiagnostics.OutputHealthSummary` for debugger/log use.

## Why this package also fixes build issues

The package fixes two compile/build problems found after the previous patch:

- `ViewportFramePipelineSelector` used `NprRenderContentKind` without importing `STFU.Rendering.Abstractions.Execution`.
- `ScenePanelViewModel._suspendEntityCommit` was read but never assigned; it always behaved as `false`, so the field was removed and `_isRefreshing` remains the only guard.

## Safety

This package does not enable interactive preview output by default. It does not remove Reference Quality fallback. It does not change the default pipeline strategy names. It only adds visibility into readiness and fixes the reported build blockers.
