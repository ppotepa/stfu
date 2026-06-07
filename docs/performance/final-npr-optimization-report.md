# STFU NPR Renderer Final Optimization Report

## Scope

This report closes the NPR renderer optimization program for the `D:\Git\stfu4` snapshot family.

Baseline snapshot metadata:

- Codecat version: 1
- Root: `D:\Git\stfu4`
- Total files: 730
- Total lines: 56,848
- .NET SDK: 10.0.201

## Final validation checklist

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/run-npr-final-optimization-validation.ps1 -Configuration Release -RunSweep
```

Required checks:

- `dotnet build STFU.slnx`
- `STFU.Parallelism.Tests`
- `STFU.Rendering.Abstractions.Tests`
- `STFU.Rendering.Cpu.Tests`
- `STFU.NPR.Parity.Tests`
- full CPU smoke
- GPU present smoke
- GPU readback smoke
- GPU visibility readback smoke
- default render parity
- FBX UI load smoke
- NPR benchmark sample
- optional worker/tile sweep

## Optimization areas

### InkFrame

The current codebase already contains segment planning, emit flags, path emit offsets, precomputed jitter endpoints, and layer index scratch reuse. Final acceptance requires parity after repeated large-frame to small-frame runs and stable `pathsOutput` / `segmentsOutput` counters.

### Visibility and edge classification

The CPU visibility path remains the reference. GPU visibility must remain opt-in acceleration and must record mismatch/fallback diagnostics when enabled.

### CPU stroke rasterizer

The CPU stroke rasterizer uses tile-to-segment binning for parallel rendering and this package extends the single-worker path to use the same tile-bin model instead of scanning all segments for every tile.

### Tone rasterizer

Tone rendering uses cached source coordinate maps and a same-size fast path. The final code avoids the per-row delegate closure and routes same-size and mapped rendering through explicit row kernels.

### DirectX upload path

The DX stroke pass tracks upload/recreate counters, uploaded bytes, and current instance capacity. Instance buffer growth uses a less aggressive 1.5x policy while preserving persistent buffer reuse.

## Acceptance criteria

- CPU renderer parity is stable across worker counts.
- Direct viewport does not require readback except debug/export/parity/explicit readback modes.
- GPU visibility remains acceleration, not the reference.
- Benchmark defaults are changed only after sweep data.
- `rpack inspect`, `rpack lint`, and `rpack check` pass on the final package.

## Required final commands

```text
dotnet build STFU.slnx -c Release
dotnet test STFU.slnx -c Release
.\scripts\validate-final-npr-optimization.ps1
.\scripts\validate-final-npr-optimization.ps1 -FullSweep
```

## Pipeline snapshot

```text
Scene
→ Mesh
→ Projection
→ Visibility
→ Edge Classification
→ Fragments
→ Paths
→ Simplify
→ InkFrame
→ CPU Stroke Raster
→ Tone Raster
→ DirectX Upload
→ GPU Present
→ Parity / Validation
```

## DirectX readback audit

| Mode | Expected readbacks | Actual readbacks | Pass |
|---|---|---|---|
| GPU present | 0 | TBD | TBD |
| GPU readback | >0 | TBD | TBD |
| GPU readback + visibility | >0 | TBD | TBD |

## GPU visibility parity audit

| Asset | Resolution | CPU faces | GPU faces | Match ratio | Fallback | Reason |
|---|---|---|---|---|---|---|
| suzanne.obj | 320x240 | TBD | TBD | TBD | TBD | TBD |
| walking.fbx | 320x240 | TBD | TBD | TBD | TBD | TBD |

## Worker scaling

| Asset | Resolution | Workers | Avg frame ms | Notes |
|---|---|---|---|---|
| suzanne.obj | 800x600 | 1 | TBD | baseline |
| suzanne.obj | 800x600 | 8 | TBD | parallel |
| suzanne.obj | 800x600 | 16 | TBD | high parallel |
