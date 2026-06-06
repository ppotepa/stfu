# Threading Baseline

Use this when changing worker budgets, parity-critical pipeline steps, or CPU raster paths.

## Run

```powershell
dotnet test tests\STFU.Parallelism.Tests\STFU.Parallelism.Tests.csproj -v minimal
dotnet test tests\STFU.Rendering.Abstractions.Tests\STFU.Rendering.Abstractions.Tests.csproj -v minimal
dotnet test tests\STFU.NPR.Parity.Tests\STFU.NPR.Parity.Tests.csproj -v minimal
dotnet build STFU.slnx -v minimal
```

## What to record

- worker count
- worker budget mode
- render profile
- scene or parity scenario
- structural hash
- pixel hash
- step timings for the hottest NPR passes

## Rule

Any change in a hot parallel path must keep single-thread and multi-thread hashes aligned before it is treated as complete.
