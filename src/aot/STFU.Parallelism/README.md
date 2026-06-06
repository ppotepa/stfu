# STFU.Parallelism

`STFU.Parallelism` contains small, AOT-safe helpers for deterministic CPU parallel work. It is intentionally independent from render requests, UI, DirectX, importers, logging and assets.

Public API is limited to the general primitives:

- `WorkerBudget`
- `WorkerBudgetRequest`
- `WorkerBudgetMode`
- `DeterministicParallel`
- `ParallelRange`
- `PrefixSums`

Everything else should remain `internal` until it has at least two real call sites.

## When To Use It

Use this library when render/NPR code needs bounded CPU parallelism and deterministic output:

- split an integer range into stable ranges
- write into range-local buffers
- merge range-local output in `rangeIndex` order
- derive worker counts from a shared budget policy
- build prefix sums for deterministic offsets before a merge

## When Not To Use It

Do not put request-specific schedulers here. Schedulers that know about `NprRenderRequest`, UI state, GPU adapters, DirectX, importer state, logging or asset lifetime belong in their owning runtime/rendering layer.

Do not use this library to hide non-deterministic shared writes. The range split and `rangeIndex` are deterministic; the physical execution order of `Parallel.For` is not. Deterministic callers must avoid shared append order and must merge explicitly by range index.

## Worker Budgets

`WorkerBudget.Resolve()` clamps the resolved count through `MinimumWorkers` and `MaximumWorkers`. `ExplicitWorkerCount` wins over mode selection and is clamped through the same bounds.

Current mode semantics are intentionally simple:

| Mode | Intent |
| --- | --- |
| `SingleThreadDeterministic` | Always resolve to one worker unless `MinimumWorkers` forces a larger count. |
| `BackgroundSafe` | Use about half of logical processors. |
| `Balanced` | Leave headroom for UI/runtime work. |
| `Performance` | Use most processors while leaving some headroom. |
| `MaxPerformance` | Use all logical processors except one when possible. |
| `Benchmark` | Same worker count policy as `MaxPerformance`, with the caller deciding benchmark isolation. |

## Basic Range Example

```csharp
var workerCount = WorkerBudget.Resolve(new WorkerBudgetRequest(
    Mode: WorkerBudgetMode.Performance));

DeterministicParallel.ForRanges(
    0,
    items.Length,
    workerCount,
    (startInclusive, endExclusive, rangeIndex) =>
    {
        for (var i = startInclusive; i < endExclusive; i++)
        {
            ProcessItem(items[i]);
        }
    });
```

## Range-Local Buffers And Ordered Merge

```csharp
var workerCount = WorkerBudget.Resolve(new WorkerBudgetRequest(
    Mode: WorkerBudgetMode.Performance));
var rangeCount = DeterministicParallel.GetRangeCount(items.Length, workerCount);

var counts = new int[rangeCount];
var rangeBuffers = new List<Result>[rangeCount];
for (var i = 0; i < rangeBuffers.Length; i++)
{
    rangeBuffers[i] = new List<Result>();
}

DeterministicParallel.ForRanges(
    0,
    items.Length,
    workerCount,
    (startInclusive, endExclusive, rangeIndex) =>
    {
        var buffer = rangeBuffers[rangeIndex];
        for (var i = startInclusive; i < endExclusive; i++)
        {
            if (TryBuildResult(items[i], out var result))
            {
                buffer.Add(result);
            }
        }

        counts[rangeIndex] = buffer.Count;
    });

var offsets = new int[rangeCount];
var total = PrefixSums.ExclusiveFromCounts(counts, offsets);
var merged = new Result[total];

for (var rangeIndex = 0; rangeIndex < rangeCount; rangeIndex++)
{
    rangeBuffers[rangeIndex].CopyTo(merged, offsets[rangeIndex]);
}
```

## Guard Rule

Do not call `Parallel.For`, `Parallel.ForEach` or `Parallel.Invoke` directly outside this library for render/NPR CPU work. Route range work through `DeterministicParallel` so range partitioning and diagnostics stay consistent.

Expected scan:

```powershell
rg -P "(?<!Deterministic)Parallel\.(For|ForEach|Invoke)" src/aot src/runtime/STFU.Rendering.DirectX -g "*.cs" -n
```

Expected result: only `src/aot/STFU.Parallelism/DeterministicParallel.cs`.
