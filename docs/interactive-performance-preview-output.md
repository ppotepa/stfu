# Interactive Performance preview output gates

Interactive Performance is a viewport-oriented frame pipeline strategy. Its long-term purpose is to produce a low-latency NPR frame from cached projection, visibility, candidate-edge, stroke-command, visible-segment, tone, and stroke-frame artifacts.

The safe default is still Reference Quality final output. Interactive Performance can collect and diagnose artifacts while the viewport returns the Reference Quality stroke frame. This keeps visual parity stable while the optimized path is being brought online.

## Default behavior

By default:

- `FramePipelineStrategy.InteractivePerformance` can run the interactive artifact pipeline.
- `UseReferenceFallbackForFinalFrame` remains `true`.
- `EnableInteractivePreviewOutput` remains `false`.
- Reference Quality remains the final returned `StrokeFrame`.
- Diagnostics still report artifact readiness and preview-candidate status.

This means the strategy selection can be validated without changing final pixels.

## Environment gates

The viewport options resolver reads opt-in environment variables. They are intentionally explicit so that experimental preview output cannot become active by accident.

| Variable | Values | Meaning |
| --- | --- | --- |
| `STFU_INTERACTIVE_PREVIEW_OUTPUT` | `1`, `true`, `yes`, `on` | Allows Interactive Performance to return its assembled stroke frame when one is available. |
| `STFU_INTERACTIVE_FORCE_REFERENCE_FALLBACK` | `1`, `true`, `yes`, `on` | Forces Reference Quality final output even if preview output is otherwise enabled. |
| `STFU_INTERACTIVE_PREVIEW_REQUIRE_TONE` | `1`, `true`, `yes`, `on` | Requires tone coverage before the interactive frame is allowed to become final viewport output. |
| `STFU_INTERACTIVE_PREVIEW_MAX_SEGMENTS` | positive integer | Caps emitted interactive preview stroke segments. Values are clamped to the supported range. |
| `STFU_INTERACTIVE_PREFER_SELF_CONTAINED_PROJECTION` | `1`, `true`, `yes`, `on` | Forces the self-contained projection preference regardless of direct presentation state. |

Invalid or empty values are ignored and fall back to safe defaults.

## Decision order

Interactive preview output is selected only when all required gates pass:

1. `ForceReferenceFallback` must be false.
2. `UseReferenceFallbackForFinalFrame` must be false.
3. `EnableInteractivePreviewOutput` must be true.
4. An interactive stroke frame artifact must exist.
5. The artifact must contain renderable paths and segments.
6. If tone coverage is required, tone regions must exist.

Each failed gate produces an `InteractivePreviewDecisionKind` and a readable reason. The reason is stored in diagnostics as the final output reason.

## Readiness ladder

The output selector reports a readiness ladder so the UI and logs can show how far the interactive pipeline progressed:

- `None`
- `ProjectionReady`
- `VisibilityReady`
- `CandidateEdgesReady`
- `StrokeCommandsReady`
- `VisibleSegmentsReady`
- `StrokeFrameReady`
- `PreviewReady`

The readiness score maps these states to 0, 10, 25, 40, 55, 70, 85, and 100. This is diagnostic only; it is not a quality score and should not drive visual styling.

## Validation expectations

When preview output is disabled, expected counters are:

- `InteractivePerformance.returnedReferenceFallback = 1`
- `InteractivePerformance.returnedInteractiveFrame = 0`
- `InteractivePerformance.previewDecision` is not `SelectedInteractiveFrame`
- readiness may still advance if artifacts were built

When preview output is enabled and an interactive frame is valid, expected counters are:

- `InteractivePerformance.returnedInteractiveFrame = 1`
- `InteractivePerformance.returnedReferenceFallback = 0`
- `InteractivePerformance.returnedInteractiveFramePaths > 0`
- `InteractivePerformance.returnedInteractiveFrameSegments > 0`

Reference Quality remains the export/parity baseline. Interactive preview output is for viewport experimentation until visual parity and performance benchmarks are proven.
