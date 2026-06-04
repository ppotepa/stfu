# STFU agent context

This is the short context packet for LLM agents. Read this before opening long docs.

## Goal

STFU is a small .NET 10 engine for loading 3D geometry, controlling a viewport camera, and producing 2D NPR drawing output from meshes. The current direction is a modular engine with AOT-friendly core libraries and runtime UI/import/plugin facilities.

## Repository shape

- `STFU.slnx`: canonical solution.
- `src/aot`: AOT-compatible engine libraries.
- `src/runtime/STFU.App`: console host.
- `src/runtime/STFU.UI`: Avalonia UI and viewport.
- `src/aot/STFU.NPR`: shared NPR contracts, graph, styles, settings, pipeline interfaces.
- `src/aot/pipelines`: concrete NPR pipelines.
- `src/aot/STFU.NPR.Preset.*`: style-only preset packs.
- `docs`: theory and implementation notes.
- `maquettes`: HTML/CSS/JS prototypes.
- `assets`: local large assets.

## Current architecture decisions

- AOT code uses explicit composition and avoids reflection-based discovery.
- Runtime code may use reflection, Avalonia, and external/native import tooling.
- Presets do not create pipelines.
- Pipeline providers create pipelines and may expose built-in presets.
- NPR pipeline work should start in a pipeline project unless it is clearly shared.

## Current validation

Default:

```powershell
dotnet build STFU.slnx -v minimal
```

Tests are intentionally minimal for now unless a task asks for them or a stable shared contract needs coverage.

## Files to avoid unless needed

- `concat.txt`
- `concat.zip`
- large files under `assets`
- `docs/NPR_SUPPLEMENT_IMPL.md`
- `third_party/ufbx`
- `bin` and `obj`

Use `tools/agent/*.ps1` before broad file reads.
