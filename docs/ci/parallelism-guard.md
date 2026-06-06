# Parallelism guard scan

Direct render/NPR use of `Parallel.For`, `Parallel.ForEach` and `Parallel.Invoke` is blocked outside `STFU.Parallelism`.

Run from the repository root:

```powershell
rg -P "(?<!Deterministic)Parallel\.(For|ForEach|Invoke)" src/aot src/runtime -g "*.cs" -n
```

Or use the checked-in guard:

```powershell
powershell -NoProfile -File tools/ci/guard-parallelism.ps1
```

Expected result:

```text
src/aot/STFU.Parallelism/DeterministicParallel.cs:<line>:        Parallel.For(
```

Any other result means the call site should be moved behind `DeterministicParallel` or explicitly justified in code review.

Audit-only scan:

```powershell
rg -P "\b(new\s+Thread|Task\.Run|ThreadPool\.QueueUserWorkItem)\b" src/aot src/runtime -g "*.cs" -n
```

This scan is informational. Thread-owned schedulers and background import workers are allowed when the owning layer justifies them.
