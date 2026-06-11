# DX11 GPU timing

`Stopwatch` around a Direct3D11 call is not the same as GPU execution time.

Current policy:
- when timestamp queries are available and enabled, `DirectXGpuTimer` reports `GpuTimestamp`,
- otherwise it reports CPU wall time fallback,
- CPU wall fallback must be labeled as fallback in diagnostics and must not be described as true GPU time.

Important implication:
- upload, draw and readback timings can include CPU-side waiting,
- readback and `Map` can synchronize CPU with GPU,
- GPU-present path should not force readback just to collect timing.

True GPU timestamp query coverage is the authoritative path for future DX11 timing work.
