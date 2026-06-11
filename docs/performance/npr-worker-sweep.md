# NPR Worker Sweep

Use `tools/bench/npr-worker-sweep.ps1` to compare `ReferenceQuality` and related CPU/GPU smoke paths across worker counts.

Current script behavior:
- uses `--smoke-fullcpu` by default,
- supports `--smoke-gpu-present` and `--smoke-gpu-readback`,
- passes worker count via `--workers`,
- supports `--npr-range-timings` through the script `-RangeTimings` switch,
- does not pass `--asset`, because the current app CLI does not expose that flag on smoke commands.

Expected output to inspect:
- total frame timing,
- pipeline timing,
- per-step notes,
- worker count,
- step counters,
- range summaries when `-RangeTimings` is enabled.
