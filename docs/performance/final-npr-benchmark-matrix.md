# Final NPR Benchmark Matrix

## Goal

Validate final renderer performance after projection, topology, visibility, InkFrame, CPU raster, tone raster, DirectX upload, and GPU visibility hardening.

## Assets

| Asset | Purpose |
|---|---|
| `assets/suzanne.obj` | Small stable OBJ baseline |
| `assets/walking.fbx` | Animated/import-heavy FBX baseline |
| `assets/Goku.obj` | Heavier stylized model stress case |

## Resolutions

| Resolution | Purpose |
|---|---|
| `320x240` | Smoke / fast regression |
| `800x600` | Default development baseline |
| `1280x720` | HD baseline |
| `1920x1080` | Full HD stress |

## Workers

| Workers | Purpose |
|---|---|
| `1` | Deterministic sequential baseline |
| `8` | Moderate parallelism |
| `16` | High parallelism / common desktop target |

## Tile sizes

| Tile size | Purpose |
|---|---|
| `16` | Small tile overhead / cache pressure |
| `32` | Default expected sweet spot |
| `64` | Large tile coarse binning |

## Modes

| Mode | Required command |
|---|---|
| CPU | `--smoke-fullcpu` / `--bench-render-profiles` |
| GPU present | `--smoke-gpu-present` |
| GPU readback | `--smoke-gpu-readback` |
| GPU visibility | `--smoke-gpu-readback --gpu-visibility` |
| NPR parity | `--verify-render-parity default 320 240 3` |

## Release gate

The release candidate is acceptable only when:

- all tests pass,
- CPU worker parity passes,
- GPU present smoke passes,
- GPU readback smoke passes,
- GPU visibility smoke does not crash,
- direct viewport readback contract remains observable,
- final benchmark results are recorded in `docs/performance/final-npr-optimization-report.md`.
