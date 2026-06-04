# NPR architecture brief

Read this instead of long NPR theory docs for normal implementation work.

## Core model

The NPR system is intended to transform scene geometry into layered 2D drawing data:

```text
mesh + camera + style -> graph -> strokes/tone/layers -> viewport/export
```

The project should support multiple pipeline implementations without mixing pipeline construction with style presets.

## Important shared types

- `INprPipeline`: executable pipeline contract.
- `INprStep`: pipeline step contract.
- `NprPipeline<T1...T20>`: AOT-friendly typed step pipeline variants.
- `NprContext`: per-frame context containing scene, view, settings, style, graph, debug data, and output frame.
- `NprGraph`: frame-local graph data such as projected geometry, feature curves, visibility segments, candidates, strokes, tone surfaces.
- `INprPreset`: metadata, settings, grammar, style set, and `PipelineId`.
- `INprPipelineProvider`: `PipelineId`, `CreatePipeline()`, optional `CreateBuiltInPresets()`.
- `ActiveNprPresetState`: active preset/settings/grammar/style set/pipeline resolved by `PipelineId`.

## Placement rules

- Shared contracts and stable data: `src/aot/STFU.NPR`.
- Concrete pipeline algorithm: `src/aot/pipelines/STFU.NPR.Pipeline.<Name>`.
- Style-only preset pack: `src/aot/STFU.NPR.Preset.<Name>`.
- UI controls for presets/layers/debug: `src/runtime/STFU.UI`.

## Current pipeline policy

Old experimental steps were intentionally removed. Do not bring them back without explicit request.

New pipelines should start small and deterministic:

```text
Project geometry
Build visible surface/tone data
Extract contour/crease/accent evidence
Create layer-aware drawing primitives
Build viewport/export frame
```

Promote helpers into `STFU.NPR` only when multiple pipelines require them.

## Style model

Styles should own drawing decisions, not geometry extraction:

- foreground/midground/background role behavior
- layers
- stroke channels: contour, crease, accent
- shading channels: fill, tones, hatching
- per-layer opacity/order/detail scale

Entity-level overrides should override style role settings without changing the pipeline implementation.

## Validation

Use:

```powershell
dotnet build STFU.slnx -v minimal
```

For visual bugs, inspect logs and minimal viewport state before reading large docs.
