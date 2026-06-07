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
