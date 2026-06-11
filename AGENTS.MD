# AGENTS.md - STFU token-safe workflow

Agent contract: precise edits, low tokens, cheap checks, safe handoff. CodeGraph first; `rg` fallback/guard; small slices/patches; `.NET` only at checkpoints; no parallel `.NET`.

# 1. Repository Role

Perf-sensitive .NET/C# rendering/NPR app: AOT, CPU raster, DirectX, UI bridge, native FBX. Patch source, not generated output. Source: `src/`, `tests/`, `tools/`, `docs/`, `maquettes/`. Context: `artifacts/`, `logs/`, `release/`, `bin/`, `obj/`, `TestResults/`, `coverage/`, `BenchmarkDotNet.Artifacts/`.

# 2. Tool Priority

Order: CodeGraph, `rg`, small slice, `apply_patch`, `git diff -- <file>`, cheap guards, focused build/test, final gates. Do not start with full files, full build, or recursive repo listing.

# 3. CodeGraph MCP First

Use for architecture, symbols, callers/callees, impact, related files, surveys, affected tests. Tools: `codegraph_status`, `codegraph_explore`, `codegraph_search`, `codegraph_callers`, `codegraph_callees`, `codegraph_impact`, `codegraph_node`, `codegraph_files`. Grep only if stale/edited/conflicting/anchor-needed. If unavailable: `CODEGRAPH_UNAVAILABLE`, then `rg`. If unindexed: `codegraph init -i`; stale: status/sync. Do not inspect `.codegraph/`.

# 4. CodeGraph + Patch

Flow: status, explore/search, impact for public/high-fanout, one slice, patch, diff, sync if needed, cheap validation. Patch target files only.

# 5. Read Budget

Per file: one CodeGraph result, one `rg`, one 30-80 line slice. No full file over 120 lines unless rewrite, missing anchor, CodeGraph failure, or user asks. Prefer `rg`; slice with `Select-Object -Skip N -First 80`.

# 6. Patch Card

Use: `FILE`, `OPERATION`, `READ BUDGET`, `CHECK FIRST`, `SKIP IF`, `ANCHOR`, `OPTIMAL OPERATION`, `CODE`, `VALIDATE`. Write surgical ops: one method, helper, counter, field, guard. Avoid vague "improve renderer".

# 7. Idempotency

Before add/insert/replace: verify final symbol, anchor, insertion point, signature, absence of final logic. Outcomes: `DONE`, `SKIPPED_ALREADY_PRESENT`, `ANCHOR_MISSING`, `SNAPSHOT_DIVERGED`, `PUBLIC_API_CONFLICT`, `DEPENDENCY_CYCLE_RISK`, `NEEDS_HUMAN_DECISION`. Missing anchor: stop.

# 8. Patch Size

Good: one file, one symbol, one purpose. Acceptable: 2-4 connected files, one feature slice. Risky: unrelated files, formatting plus logic, public API plus impl plus tests plus docs. Split risky work.

# 9. Chapter Workflow

Chapter: goal, files, CodeGraph/anchors, idempotent ops, cheap validation, report, stop before expensive validation unless requested. Report: `DONE`, `SKIPPED`, `VALIDATION`, `RISKS`, `NEXT`. No transcripts unless asked.

# 10. Cost Model

Low: CodeGraph reads, `rg`, file diff, status, branch/remote reads, diff check, slice. Medium: one project build/test, source guard, small smoke. High: full build/suite/format, benchmark, publish, AOT/native, integration smoke. High cost only final/on request.

# 11. Parallel Tools

Parallel only independent reads: CodeGraph, `rg`, slices, listings, git status, docs. Never parallelize mutating/output commands: `git add`, commit, checkout, merge, rebase, apply, overlapping patches, build, test, format, publish, restore, codegen, install.

# 12. Git

Before edit: `git status --short`. After patch: `git diff -- <file>`. Before staging: `git diff --check`. Final order: status, diff check, `git add -A`, cached stat, commit, then `git rev-parse --short HEAD`. Do not parallelize mutating git.

# 13. Validation

Tier 0: symbol/counter, file diff. Tier 1: diff check, guards. Tier 2: focused build for signatures/imports/project/public contracts. Tier 3: focused tests. Tier 4: final build, format, affected tests, smoke/bench if required.

# 14. Affected Tests

Prefer `git diff --name-only | codegraph affected --stdin --quiet`. Empty affected list plus public contract change still needs focused build. Broaden on dependency direction changes.

# 15. Stop Conditions

Stop/report on missing anchors, unavailable CodeGraph without safe fallback, symbol drift, divergent snapshot, public API conflict, dependency cycle risk, generated-file risk, unknown test infra. Do not patch similar-looking code.

# 16. Generated Files

Do not edit generated outputs: `bin/`, `obj/`, `target/`, `dist/`, `build/`, `artifacts/`, `logs/`, `release/`, `TestResults/`, `coverage/`, `BenchmarkDotNet.Artifacts/`, `node_modules/`.

# 17. Dependencies

Check layer direction, existing abstraction, downward contract move, internal helper first. Low-level projects must not depend on runtime/UI/backend. If unclear or cyclic: `DEPENDENCY_CYCLE_RISK`.

# 18. Performance

No blind optimization. Include hot path, hypothesis, metric/counter, patch, cheap validation, final plan. Order: counters, allocations, scratch reuse, redundant work, partitioning, deterministic merge, layout, GPU/native when justified. Optimize frame time, latency, memory, responsiveness, determinism.

# 19. Concurrency

Avoid raw `Parallel.For`, `Parallel.ForEach`, `Parallel.Invoke`, `Task.Run`, `new Thread`, `ThreadPool.QueueUserWorkItem` in hot code. Use approved wrappers. Define partitioning, stable range/merge order, cancellation, exceptions, deterministic output, validation.

# 20. STFU Context

Important: `src/aot/`, `src/runtime/`, `src/native/`, `tests/`, `tools/`, `docs/`, `maquettes/`. Sensitive: NPR pipelines, CPU raster, DirectX, UI bridge, parallelism, rendering abstractions. Patch `maquettes/` only for mockups.

# 21. STFU CodeGraph

Use CodeGraph before broad exploration. Examples: NPR->DirectX, CPU raster bins, worker budget. Use `rg` for final strings, counters, forbidden patterns, anchors.

# 22. Approved Wrappers

Parallelism: `STFU.Parallelism`, `DeterministicParallel`, `WorkerBudget`, `WorkerBudgetRequest`, `WorkerBudgetMode`, `PrefixSums`, `ParallelRange`. Runtime/render: `LatestNprRenderScheduler`, `NprFrameBudget`, `NprRenderDiagnostics`, context factories. Generic parallelism must not depend on render/UI/DirectX/importer/logging/assets.

# 23. Forbidden Raw Parallelism

Forbidden outside approved wrappers/schedulers: `Parallel.For`, `Parallel.ForEach`, `Parallel.Invoke`, `Task.Run`, `new Thread`, `ThreadPool.QueueUserWorkItem`. Exceptions need docs and source guard tests. Approved areas: `STFU.Parallelism`, `LatestNprRenderScheduler`, documented runtime schedulers.

# 24. Cheap STFU Validation

Use `git diff --check`, `git diff -- <file>`, `rg` expected symbol/counter, `rg` forbidden parallel/thread patterns. Prefer `tools/ci/guard-parallelism.ps1` if present.

# 25. .NET

Expensive. Flow: source check, diff, focused build/test, final gate. Respect `global.json`. CodeGraph, fallback `rg`. Patch one method/helper/field/counter/test. Avoid broad format, needless API, namespace cleanup, unrelated nullable work, mass rename. Never parallelize `dotnet build/test/format/publish/restore`. Final build: `dotnet build STFU.slnx -c Release --no-restore -m:1`. Format once. Tests sequentially, preferably `--no-build`. Lock recovery: stop, wait, rerun sequential, `-m:1`, clean last. Project files are build contracts. Final order: diff check, format, build, sequential Parallelism/NPR/CPU/DirectX tests; smoke/bench only for runtime proof.

# 26. RPACK

If requested, deliver `.rpack`: ZIP, root `manifest.json`, `patches/...`, checksums, repo root path. Before apply: `rpack inspect`, `rpack lint`, `rpack check`, then `rpack apply` or `rpack open`.

# 27. Final Report

Report `DONE`, `SKIPPED`, `VALIDATION`, `RISKS`, `NEXT`. No huge diffs or transcripts unless asked.

# 28. Patch Plan

Use `TOKEN-SAFE PATCH PLAN`: chapter, goal, files, CodeGraph first, read budget, operations with file/op/check/skip/anchor/code/validate, do-not list, final gate. Mechanical enough to avoid exploration.

# 29. Research / Review

CodeGraph first. Separate confirmed, inferred, not established, next checks. Generated artifacts/logs are not source truth unless stated.

# 30. Highest Rule

Small verified symbol operations; no broad exploration when graph works; no whole files when slices suffice; no expensive validation while editing; no parallel mutating commands; no scope expansion without permission.
