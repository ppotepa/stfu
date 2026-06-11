# STFU NPR Renderer Final Optimization Report

Date: 2026-06-07  
Branch: `master`  
Commit: `8a6789d`  
Root: `D:\Git\stfu4`

## Scope

- Final release hardening gate and runtime proof for NPR renderer contracts.
- Finalization focuses on release validation, diagnostics correctness, and risk closure.

## Snapshot

- Files: `757`
- Total lines: `59,158`
- C# files: `621`
- PowerShell scripts: `46`
- Docs: `4`
- .NET SDK: `.NET 10.0.201`

## Hardware / runtime

- OS: Windows (direct run, local host)
- GPU: NVIDIA GeForce RTX 3070 Ti (`featureLevel=Level_11_1`)
- Processor count: `16`
- Runtime: `.NET 10.0.5`
- Build config: `Release`

## Final validation checklist

- `dotnet build STFU.slnx` (Release): pass
- `dotnet test STFU.slnx -c Release`: pass
- Project checks:
  - `STFU.Rendering.Abstractions.Tests`: pass
  - `STFU.Rendering.Cpu.Tests`: pass
  - `STFU.NPR.Parity.Tests`: pass
  - `STFU.NPR.Pipelines.Tests`: pass
  - `STFU.Rendering.DirectX.Tests`: pass
- Hot-path audit + parallelism guard: pass (non-blocking list allocations)
- Final optimization validation script: pass (`tools/ci/run-npr-final-optimization-validation.ps1`)
- Runtime DirectX diagnostic test: pass (`NprDirectXRuntimeDiagnosticsTests`)

## Final commands

```text
dotnet build STFU.slnx -c Release
dotnet test STFU.slnx -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/run-npr-final-gate.ps1
```

### Gate run observed

- `run-npr-final-gate.ps1` (with default parameters) completed with exit code `0`.

## Renderer pipeline

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

## Projection status

Stable and complete for this gate; no regression blockers found.

## Topology status

Stable and complete for this gate; no regression blockers found.

## Visibility status

- CPU remains reference baseline.
- GPU visibility is opt-in and reports parity/fallback diagnostics.

## InkFrame status

Segment planning and scratch reuse are in place; no blocking issues in release gate.

## CPU stroke raster status

Tile binning and parallel paths validated by tests and runtime; benchmark coverage in place.

## Tone raster status

Same-size and mapped raster paths present; benchmark and test gate pass.

## DirectX upload status

Upload/recreate counters and allocation telemetry are present and used in diagnostics.

## DirectX readback / Present status

- Runtime contract test verifies:
  - direct present path uses `NprExecutionProfile.CpuDrivenGpuAccelerated`
  - output `GpuTexture`
  - `Diagnostics.Readbacks == 0`
- Runtime readback path verifies:
  - output `PixelSurface`
  - `Diagnostics.Readbacks > 0`
- Runtime GPU readback smoke verifies readback execution and fallback counters in render output logs.

### DirectX readback audit

| Mode | Expected readbacks | Actual result (runtime proof) | Pass |
|---|---|---|---|
| GPU present | `0` | `Readbacks == 0` (from runtime diagnostics test) | PASS |
| GPU readback | `> 0` | `Readbacks > 0` (from runtime diagnostics test) | PASS |
| GPU readback + visibility | `> 0` | `edgeSampleReadback` present in smoke logs; runtime diagnostics path executed | PASS |

## GPU visibility parity/fallback

- Runtime test executes `NprDirectXRuntimeDiagnosticsTests.DirectXBackend_RuntimeDiagnostics_ReportReadbacksAndVisibility`.
- For `suzanne` with `GpuVisibilityRequiredMatchRatio=1f`, visibility stats were emitted and accepted.
- Sample smoke metrics:
  - `cpuVisible=538`, `gpuVisible=538`, `mismatches=0`
  - `edgeSamples=1494`, `edgeVisible=1492`, `edgeMismatches=2`
- `FallbackUsed` contract remains respected (when mismatch threshold requires fallback, path transitions to CPU).

### GPU visibility parity audit

| Asset | Resolution | CPU faces | GPU faces | Match ratio | Fallback | Reason |
|---|---|---|---|---|---|---|
| suzanne.obj | 320x240 | 538 | 538 | `1.0000` | false | none |
| walking.fbx | 320x240 | (not sampled in runtime smoke; parity tests passed for pipeline) | (not sampled) | not sampled | not sampled | not sampled |

## Parity result

- `tools/parity/test-results/.last-run.json`: `{"status":"passed","failedTests":[]}`
- `test-results/.last-run.json` did not move during active gate and was stale from earlier infra run; file removed and documented below.

## Hot-path audit result

- `artifacts/npr-hotpath-audit.txt` generated during gate and reviewed.
- No `Parallel.For`/`Parallel.ForEach` in hotpaths outside `DeterministicParallel.ForRanges`.
- Remaining `new List<>`/`new Dictionary<>` hits are in setup/scratch/debug paths and not treated as blockers.
- `Thread.Run/Thread` hits reported by `guard-parallelism.ps1` are in known non-hot infrastructure paths (`STFU.Import.Fbx`, `LatestNprRenderScheduler`) and accepted as non-gate-blocking.

## Smoke results (320x240, Release)

- Full CPU smoke passed.
- GPU present smoke passed (`output=GpuTexture` for default path).
- GPU readback smoke passed (`output=PixelSurface`).
- GPU visibility readback smoke passed (`Visibility` details present in logs).
- Render parity checks passed (`--verify-render-parity default 320 240 3` and `--gpu-visibility` variant).
- FBX UI load smoke passed for `walking.fbx`.

## NPR benchmark sample

### `assets\walking.fbx` / `800x600` / `60` frames / `warmup 10` / `--animation fixed-step`

- `cpu`: `avgTotal=23.69ms` (42.2 FPS)
- `cpu-gpu-direct`: `avgTotal=8.92ms` (112.1 FPS)
- `cpu-gpu-readback`: `avgTotal=8.40ms` (119.0 FPS)

## Worker scaling

### `assets\suzanne.obj` / `800x600` / `default` / `30` frames / `warmup 5`

| Workers | Tile | `cpu` avgTotal | `cpu-gpu-direct` avgTotal | `cpu-gpu-readback` avgTotal |
|---|---|---|---|---|
| 1 | 32 | 23.96ms | 4.65ms | 5.20ms |
| 8 | 32 | 20.91ms | 6.58ms | 6.01ms |
| 16 | 32 | 26.71ms | 7.34ms | 3.42ms |

## Tile scaling

### `assets\suzanne.obj` / `800x600` / `workers=1`

| Tile size | `cpu` avgTotal | `cpu-gpu-direct` avgTotal | `cpu-gpu-readback` avgTotal |
|---|---|---|---|
| 16 | 16.03ms | 4.31ms | 4.71ms |
| 32 | 23.96ms | 4.65ms | 5.20ms |
| 64 | 12.21ms | 5.74ms | 4.27ms |

## Known risks

- `tools/parity/test-results/.last-run.json` is the active parity artifact; root-level `test-results/.last-run.json` is stale in this repo snapshot and should not be used.
- `STFU.NPR.Parity.Tests` emits one nullable warning (`CS8602`) in `NprDirectXRuntimeDiagnosticsTests.cs` but no runtime failure.
- `walking.fbx` + direct GPU visibility parity with `320x240` was not separately sampled in a dedicated smoke variant for the final matrix (rely on parity suite + diagnostics assertions).
- Full 24-worker and broader matrix sweep (`--workers 24` and full cross-asset/resolution matrix) is not executed in this pass.

## Release recommendation

**RELEASE OK WITH KNOWN RISKS**

- Gate/build/tests smoke/parity passed.
- DirectX readback and visibility runtime contracts are verified via runtime diagnostics tests and smoke.
- Hot-path audit accepted with minimal follow-up non-blocking exclusions.
- Full extended sweep is recommended post-release for optimization headroom, but no blocker for hardening closure in this iteration.
