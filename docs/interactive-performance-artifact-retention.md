# Interactive Performance artifact retention

Interactive Performance keeps a small artifact store across frames so camera and viewport work can reuse recent projection, visibility, candidate, stroke, tone, and preview-frame artifacts.

The store must stay bounded because frame/camera artifacts are keyed by content, camera, style, viewport, and quality signatures. Orbiting a camera can otherwise create a new set of frame-scoped artifacts on every frame.

## Retention policy

`ArtifactStore.PruneFrameOrCameraArtifacts(maxPerKind, maxTotal)` removes stale `ArtifactLifetime.FrameOrCamera` artifacts after output selection and before diagnostics capture.

The policy is intentionally conservative:

- static, scene, and session artifacts are never removed by this pruning pass;
- for each `ArtifactKind`, the newest revisions are kept first;
- after per-kind retention, an optional global cap trims the remaining frame/camera artifacts;
- output selection happens before pruning, so the selected artifact can still be referenced by the current `InteractivePipelineResult`.

Default retention is configured in `FramePipelineStrategyOptions`:

- `MaxFrameOrCameraArtifactsPerKind = 3`
- `MaxTotalFrameOrCameraArtifacts = 64`

## Viewport environment overrides

The viewport resolver exposes two optional environment variables:

- `STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_PER_KIND`
- `STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_TOTAL`

Invalid, empty, or non-positive values fall back to defaults. Values are clamped to safe ranges.

## Diagnostics

The pruning pass writes:

- `InteractivePerformance.prunedFrameOrCameraArtifacts`
- `InteractivePerformance.artifactStoreItems`
- `InteractivePerformance.frameOrCameraArtifacts`

These counters make cache growth visible while preserving the safe Reference Quality fallback behavior.

## Current limitation

This retention pass does not yet implement semantic invalidation for deeper scene graph changes. That should come later as explicit artifact lifetimes and scene/content epochs mature.
