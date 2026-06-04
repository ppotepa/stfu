# NPR pipeline agent instructions

This subtree contains standalone NPR pipeline projects.

Pipeline projects own:

- concrete `INprStep` implementations
- concrete `INprPipelineProvider`
- pipeline-specific built-in presets
- pipeline-specific helpers that are not stable shared abstractions yet

Keep preset/pipeline separation:

- A pipeline provider creates the pipeline.
- A preset configures settings, grammar, style set, and `PipelineId`.
- Promote code to `src/aot/STFU.NPR/` only when multiple pipelines need it.

When implementing a new pipeline, prefer:

```text
src/aot/pipelines/STFU.NPR.Pipeline.<Name>/
  <Name>Pipeline.cs
  <Name>PipelineProvider.cs
  <Name>Preset.cs
  Steps/
```

Do not read long theory docs by default. Use `docs/NPR_ARCHITECTURE_BRIEF.md` first.
