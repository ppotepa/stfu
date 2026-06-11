# Parallelism guard

Allowed:
- `STFU.Parallelism.DeterministicParallel` for deterministic range loops.
- `LatestNprRenderScheduler` for render request lifecycle and thread ownership.
- Low-level DirectX device and context calls inside the DirectX backend when already serialized by the device lock.

Forbidden by default:
- raw `Parallel.For`, `Parallel.ForEach`, `Parallel.Invoke` in `src/aot` and render runtime,
- `Task.Run` in hot path code,
- `new Thread` in render or NPR pipeline code,
- concurrent writes to graph lists without deterministic partition and merge.

Run:

```powershell
powershell -NoProfile -File tools/ci/guard-parallelism.ps1
```

If a new exception is required, add it to this document and tests first.
