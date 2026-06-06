# Threading And Parallelism

This document defines where STFU owns threading and parallel work splitting.

## Goals

- Keep deterministic render output across worker counts.
- Keep AOT code explicit and reflection-free.
- Keep parallel work helpers centralized.
- Keep long-lived runtime threads separate from per-frame work splitting.

## Ownership

`src/aot/STFU.Parallelism/` is the canonical shared location for CPU worker
budgets and deterministic range partitioning.

Use it for:

- CPU render raster ranges
- NPR pipeline range processing
- deterministic per-frame CPU work
- benchmark worker budgets

Do not add direct `Parallel.For`, `Parallel.ForEach`, or `Parallel.Invoke`
outside `STFU.Parallelism` for render/NPR work. Use
`DeterministicParallel.ForRanges` instead.

## Worker Budgets

Render requests carry worker configuration through `NprFrameBudget`:

- `WorkerBudgetMode`
- `MaxWorkerThreads`
- `EnableTileParallelism`
- `TileSize`

Resolve worker counts once per frame or context with
`NprFrameBudget.ResolveWorkerCount()`. Downstream code should consume
`NprContext.WorkerCount` or the resolved count passed by the backend instead of
reading `Environment.ProcessorCount` directly.

UI settings live in renderer settings:

- `RendererSettingsSnapshot`
- `RendererSettingsViewModel`
- `SettingsWindow`
- `ViewportRenderBridge`

The UI default is `WorkerBudgetMode.Performance`, `MaxRenderWorkers = 0`, and
`EnableTileParallelism = true`.

## Deterministic Range Work

Use:

```csharp
DeterministicParallel.ForRanges(
    0,
    itemCount,
    workerCount,
    (start, end, rangeIndex) =>
    {
        for (var i = start; i < end; i++)
        {
            // Work on stable range.
        }
    });
```

If the caller needs one scratch buffer per range, use
`DeterministicParallel.GetRangeCount(...)` first and allocate exactly that many
buffers.

Parallel range bodies must not append directly into shared output lists. Use
range-local buffers, then merge in `rangeIndex` order.

## Runtime Threads

Long-lived scheduler threads are runtime coordination, not per-frame work
splitting. Render queues use `LatestNprRenderScheduler` from
`src/aot/STFU.Rendering.Abstractions/Execution/`.

Do not add backend-local `Thread` loops for render scheduling. Wrap
`LatestNprRenderScheduler` and keep backend-specific logging in the wrapper.

Runtime/import systems may still use `Task.Run`, events, or cancellation when
they own process/UI/GPU/import lifetimes.

Current approved runtime-owned workers:

- `LatestNprRenderScheduler`: shared latest-request-wins render queue.
- `FullCpuRenderScheduler`: full CPU backend adapter over `LatestNprRenderScheduler`.
- `ProfiledNprRenderScheduler`: DirectX/profiled adapter over `LatestNprRenderScheduler`.
- `FbxAnimationPrebakeCache`: background FBX animation sample cache.

These workers should log lifecycle failures and cancellation, but should not use
`DeterministicParallel` unless they are splitting a CPU work range internally.

## AOT Rules

In `src/aot`:

- prefer `STFU.Parallelism` helpers
- avoid runtime discovery, reflection, and dynamic task orchestration
- keep scheduling policies explicit in request/context data
- do not reference Avalonia or runtime import/UI code

In `src/runtime`:

- UI may expose worker settings
- render bridge may translate settings into `NprFrameBudget`
- GPU/import schedulers may own long-lived process-specific workers

## Validation

After changing parallel render or NPR code, run:

```powershell
.\tools\agent\agent.ps1 build --format json
dotnet run --no-build --project src\runtime\STFU.App\STFU.App.csproj -- --verify-render-parity default 800 600 3
dotnet run --no-build --project src\runtime\STFU.App\STFU.App.csproj -- --verify-render-parity debug-feature-curves 800 600 3
```

Use this scan before review:

```powershell
rg -P "(?<!Deterministic)Parallel\.(For|ForEach|Invoke)" src\aot src\runtime\STFU.Rendering.DirectX -g "*.cs" -n
```

Only `src/aot/STFU.Parallelism/DeterministicParallel.cs` should contain raw
`Parallel.*` calls for render/NPR work.
