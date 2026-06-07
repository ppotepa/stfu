# STFU Render Hot Path Completion Pack 004

This rpack groups the remaining optimization infrastructure requested for:

- Deep InkFrame planning contracts.
- CPU stroke tile-binning diagnostics and tone scratch reuse.
- DX11 readback counters and GPU timing scaffold.
- GPU visibility parity/fallback contracts.
- Allocation benchmark contracts and final benchmark script.

The package is intentionally split into multiple rpack patches so failures point to the exact optimization area.

## Validation

```powershell
rpack check stfu-render-hotpath-completion-004.rpack
rpack apply stfu-render-hotpath-completion-004.rpack

dotnet build STFU.slnx -c Debug

dotnet run --project src/runtime/STFU.App/STFU.App.csproj -- --verify-render-parity default 320 240 3
dotnet run --project src/runtime/STFU.App/STFU.App.csproj -- --smoke-fullcpu 320 240
dotnet run --project src/runtime/STFU.App/STFU.App.csproj -- --smoke-gpu-present 320 240

powershell -ExecutionPolicy Bypass -File scripts/bench-render-hotpath-final.ps1
```
