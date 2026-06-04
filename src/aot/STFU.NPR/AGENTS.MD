# STFU.NPR agent instructions

This project contains shared NPR abstractions and data contracts.

Preserve these invariants:

- `INprPreset` owns metadata, `PipelineId`, settings, grammar, and style set.
- `INprPreset` must not create pipelines.
- `INprPipelineProvider` owns `PipelineId`, `CreatePipeline()`, and optional `CreateBuiltInPresets()`.
- `ActiveNprPresetState` resolves pipelines by `preset.PipelineId`.
- Shared graph/settings/style types live here only when multiple pipelines need them.

Do not reintroduce deleted old pipeline steps into this project. New algorithmic steps should start in `src/aot/pipelines/STFU.NPR.Pipeline.<Name>/`.

Useful focused searches:

```powershell
.\tools\agent\search.ps1 "INprPreset|INprPipelineProvider|ActiveNprPresetState|NprContext"
.\tools\agent\search.ps1 "FeatureCurve|StyledStroke|NprFrame|NprStyleSet"
```

Default validation:

```powershell
dotnet build STFU.slnx -v minimal
```
