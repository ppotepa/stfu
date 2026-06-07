# STFU Render Hot Path Optimization Pack 003

This package intentionally groups the next optimization blocks into separate rpack patches:

- OPT-003: paths / simplify / ink prep counters.
- OPT-004: deep InkFrame emit-precompute diagnostics.
- OPT-005 + OPT-006: CPU raster workspace and sequential tile bin reuse.
- OPT-008: DX11 viewport/upload counters.
- OPT-009: GPU visibility contract additions.
- OPT-010: memory cleanup and final benchmark script.

## Safety rules

- Do not change default visual output.
- Keep CPU pipeline as parity reference.
- Keep GPU visibility opt-in.
- Avoid unbounded caches.
- Prefer reusable workspaces over global mutable state.

## Required validation

```powershell
rpack check stfu-render-hotpath-optimization-003-megapack.rpack
rpack apply stfu-render-hotpath-optimization-003-megapack.rpack
dotnet build STFU.slnx -c Debug
dotnet run --project src/runtime/STFU.App/STFU.App.csproj -- --verify-render-parity default 320 240 3
powershell -ExecutionPolicy Bypass -File scripts/benchmark_render_hotpath_optimization.ps1
```
