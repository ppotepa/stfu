# STFU NPR Renderer final optimization gate

Snapshot basis: `D:\Git\stfu4`, full concat, 744 files, 58,037 lines.

## Scope

This package is the final hardening gate for the NPR optimization program. It does not reimplement projection, topology, InkFrame, CPU tile binning, tone cache, DirectX upload counters, or GPU visibility parity. Those systems are already present in the current repository snapshot. The package adds final source-contract tests and one validation entry point.

## Covered areas

1. InkFrame planning and scratch reuse contracts.
2. CPU stroke tile binning contracts.
3. Tone raster coordinate/scratch cache contracts.
4. Visibility parity fallback policy contracts.
5. DirectX upload/readback counter contracts.
6. Hot-path allocation guard integration.
7. Final local validation script.

## Required local validation

```powershell
dotnet build STFU.slnx -c Release
dotnet test tests/STFU.Rendering.Abstractions.Tests/STFU.Rendering.Abstractions.Tests.csproj -c Release -v minimal
dotnet test tests/STFU.Rendering.Cpu.Tests/STFU.Rendering.Cpu.Tests.csproj -c Release -v minimal
dotnet test tests/STFU.NPR.Parity.Tests/STFU.NPR.Parity.Tests.csproj -c Release -v minimal
dotnet test tests/STFU.NPR.Pipelines.Tests/STFU.NPR.Pipelines.Tests.csproj -c Release -v minimal
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ci/run-npr-final-gate.ps1 -Configuration Release
```

## Non-goals

- No generated artifacts.
- No log modifications.
- No `artifacts/`, `release/`, `bin/`, or `obj/` changes.
- No projection/topology rewrite.
- No DirectX architecture rewrite.

## Final acceptance

The renderer optimization program can be considered closed when the final gate script, render parity tests, GPU smoke tests, FBX smoke tests, worker/tile sweep, and hot-path audit all pass on the Windows development machine.
