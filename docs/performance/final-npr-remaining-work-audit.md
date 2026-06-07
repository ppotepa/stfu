# STFU NPR Remaining Optimization Audit

Snapshot source: uploaded concat snapshot.

- Root: `D:\Git\stfu4`
- Files: `732`
- Lines: `57286`
- Main remaining focus: CPU stroke tile binning, tone raster cache, DX upload reuse, GPU visibility parity/fallback, allocation cleanup, final benchmark sweep.

## Current interpretation

The latest snapshot already contains the core CPU tile/tone infrastructure:

- `CpuRasterWorkspace` has tile layout cache, tile bin arrays, tone coordinate maps and tone scratch arrays.
- `CpuStrokeRasterizer` routes through tile bins and tracks stroke tile/pixel counters.
- `CpuToneRasterizer` has same-size fast path and source coordinate cache usage.
- `DirectXRenderCounters` includes upload/readback/reuse diagnostics.
- `VisibilityParityStats` includes fallback reason and CPU/GPU mismatch counters.
- `run-render-sweep.ps1` covers worker, tile-size and resolution sweep inputs.

## Remaining closure work

1. Keep regression coverage around tile layout cache and tone coordinate cache.
2. Keep GPU visibility fallback policy tests explicit.
3. Run hot-path allocation audit before final benchmark sweep.
4. Run RPACK inspect/lint/check before applying every generated package.
5. Run final build/test/smoke/parity sweep on the target Windows machine.
