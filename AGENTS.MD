# AGENTS.MD - STFU agent instructions

## Purpose

This file defines how an LLM coding agent should work in this repository.

STFU is a .NET 10 AOT/runtime hybrid for a small 3D/2D engine and NPR drawing
pipeline. The repository is large enough that agents must avoid full file and
full repo reads by default. Use the local agent toolchain first, then fall back
to raw shell tools only when the semantic tools are insufficient.

Primary goals:

- keep context small and relevant
- prefer semantic navigation over text search
- keep changes focused and reviewable
- preserve AOT/runtime and NPR architecture boundaries
- validate with the smallest reliable command that proves the change

## Prime Directive

Use the smallest reliable context needed to complete the task.

Preferred order:

1. `tools/agent/agent.ps1` semantic toolchain
2. project-aware commands such as `dotnet build`, `dotnet test`, NuGet/MSBuild
3. structured search such as Roslyn, LSP, `ast-grep`, or IDE indexes
4. text search with `rg`
5. full file reads only after targeted tools are not enough

Do not dump the whole repository, large generated files, large docs, or full
diffs unless the task explicitly requires it.

## Start Every Task

For normal code work, start with:

```powershell
.\tools\agent\agent.ps1 context --format table
```

For a code symbol, prefer:

```powershell
.\tools\agent\agent.ps1 symbols --name SomeTypeOrMember --format json
.\tools\agent\agent.ps1 member --symbol-id "..." --format markdown
```

For changed code, prefer:

```powershell
.\tools\agent\agent.ps1 diff --semantic --format json
```

For build/debug tasks, prefer:

```powershell
.\tools\agent\agent.ps1 build --format json
```

Raw fallback when the toolchain cannot answer the question:

```powershell
rg "RelevantSymbol|RelevantCommand" src -n -C 2
```

## Repository Map

Use this map before opening files:

- `STFU.slnx`: canonical solution.
- `src/aot/`: AOT-compatible engine libraries. Keep these explicit,
  deterministic, and reflection-light.
- `src/aot/STFU.Engine/`: engine builder, module registry, scene/entity basics.
- `src/aot/STFU.NPR/`: shared NPR abstractions, graph data, settings, styles,
  registry, and pipeline contracts.
- `src/aot/STFU.NPR.Preset.*`: style-only preset projects. These should not own
  pipeline construction.
- `src/aot/pipelines/STFU.NPR.Pipeline.*`: standalone NPR pipeline projects.
  These own pipeline construction and may expose built-in presets.
- `src/runtime/STFU.App/`: console host that starts the UI and logs.
- `src/runtime/STFU.UI/`: Avalonia UI and viewport host.
- `src/runtime/STFU.Import*/`: runtime import/external tooling.
- `src/native/`: native interop code.
- `assets/`: local models/assets. Avoid reading unless the task is asset,
  import, or visual validation.
- `docs/`: theory, architecture, and planning documents.
- `maquettes/`: HTML/CSS/JS UI prototypes.
- `third_party/`: vendored external code. Avoid unless the task explicitly
  targets it.
- `tools/agent/`: local semantic agent toolchain.

## Local Instructions

Read local `AGENTS.MD` files only when working in or near that subtree:

```text
assets/AGENTS.MD
docs/AGENTS.MD
maquettes/AGENTS.MD
src/aot/AGENTS.MD
src/aot/STFU.NPR/AGENTS.MD
src/aot/pipelines/AGENTS.MD
src/runtime/STFU.UI/AGENTS.MD
```

Local files are intentionally scoped. They should replace broad scanning, not
add another reason to read unrelated areas.

## Context Boundaries

Avoid reading these unless directly relevant:

- `concat.txt`
- `concat.zip`
- generated `bin/` and `obj/` trees
- `.agents/cache/`
- `.logs/agent/` except log/history tasks
- large files under `assets/`
- `third_party/ufbx/` except native FBX/import tasks
- `docs/NPR_SUPPLEMENT_IMPL.md` except architecture/theory tasks
- `maquettes/` except UI/prototype tasks

Prefer `member`, `refs`, `diff --semantic`, and `concat` over full file reads.

## AOT And Runtime Boundaries

`src/aot` projects must remain AOT-friendly:

- avoid reflection-based discovery
- avoid dynamic assembly scanning
- avoid runtime code generation
- prefer explicit registration and deterministic data flow
- do not reference Avalonia from `src/aot`
- do not place runtime plugin loading, native importer orchestration, or
  UI-specific code in `src/aot`

`src/runtime` projects may use:

- Avalonia
- reflection
- runtime plugin loading
- external/native import tooling
- console/UI process coordination

When in doubt, keep abstractions and data contracts in `src/aot`, and keep
discovery, UI, plugins, import orchestration, and process-specific behavior in
`src/runtime`.

## NPR Architecture Invariants

Preserve this split:

- `INprPreset` owns metadata, `PipelineId`, settings, grammar, and style set.
- `INprPreset` must not create pipelines.
- `INprPipelineProvider` owns `PipelineId`, `CreatePipeline()`, and optional
  `CreateBuiltInPresets()`.
- `NprModule` registers pipeline providers first, then provider built-in
  presets, then additional presets.
- `ActiveNprPresetState` resolves the active pipeline only through
  `preset.PipelineId`.
- If a preset points to an unregistered pipeline, fail loudly with a clear
  exception.

Current placement rules:

- shared NPR abstractions: `src/aot/STFU.NPR/`
- pipeline-specific implementation: `src/aot/pipelines/STFU.NPR.Pipeline.<Name>/`
- style-only preset packs: `src/aot/STFU.NPR.Preset.<Name>/`
- Avalonia viewport/UI behavior: `src/runtime/STFU.UI/`

Do not resurrect deleted old NPR step implementations unless explicitly
requested. New pipeline work should be built inside a pipeline project first;
promote code into `STFU.NPR` only when it becomes a stable shared abstraction.

## Agent Toolchain Overview

Primary entrypoint:

```powershell
.\tools\agent\agent.ps1 <command> [flags]
```

Compatibility wrappers still exist:

```powershell
.\tools\agent\context.ps1 --format json
.\tools\agent\search.ps1 --pattern NprLayerFrame --type cs
.\tools\agent\build.ps1 --project tools\agent\src\Agent.Cli\Agent.Cli.csproj
```

The PowerShell layer is only a router. The C# CLI under
`tools/agent/src/Agent.Cli` owns:

- `agent.tool.v1` JSON envelopes
- Roslyn/MSBuild workspace loading
- semantic symbol/reference/member operations
- cache reads/writes
- formatting and budget limits
- build/test/log parsing
- benchmark and preset orchestration

Stdout is data. Debug/status output belongs on stderr.

## Toolchain First Run

Check the toolchain:

```powershell
.\tools\agent\agent.ps1 doctor --format table
```

Build the CLI explicitly when needed:

```powershell
dotnet build tools\agent\src\Agent.Cli\Agent.Cli.csproj -v minimal -o tools\agent\bin
```

Inspect cache:

```powershell
.\tools\agent\agent.ps1 cache status --format json
```

Start or inspect the background server:

```powershell
.\tools\agent\agent.ps1 server start --format json
.\tools\agent\agent.ps1 server status --format table
.\tools\agent\agent.ps1 server stop --format json
```

The server preloads the workspace and warms cache. Normal commands use the warm
cache directly. Direct IPC is opt-in:

```powershell
.\tools\agent\agent.ps1 symbols --name SomeType --use-server --format json
```

Use `--refresh` to bypass stale cached data for a command.

## Common Flags

Most commands accept these flags:

```text
--format json|ndjson|table|markdown
--from-stdin
--max <n>
--context <n>
--solution <path>
--project <path|name>
--changed-only
--refresh
--cache warm
--select <field,field>
--where <expr>
--emit commands|items|summary|all
--next
--out <path>
--budget-lines <n>
--budget-tokens <n>
```

Flag usage:

- Use `--format json` for chaining.
- Use `--format table` for quick human scanning.
- Use `--format markdown` for context bundles and member snippets.
- Use `--max` and `--budget-*` before asking for broad output.
- Use `--select` to keep only fields needed by the next command.
- Use `--where` to narrow already-produced items.
- Use `--from-stdin` to chain tools.
- Use `--next` when unsure which command should follow.
- Use `--out` only for deliberate artifacts.

Every output should remain bounded. If a command truncates output, narrow the
query before increasing limits.

## JSON Envelope

All core tools return the same high-level shape in JSON:

```json
{
  "schema": "agent.tool.v1",
  "tool": "symbols",
  "ok": true,
  "root": "D:\\Git\\stfu4",
  "solution": "STFU.slnx",
  "query": {},
  "summary": {
    "count": 1,
    "returned": 1,
    "truncated": false,
    "durationMs": 67,
    "cache": "warm"
  },
  "items": [],
  "edges": [],
  "diagnostics": [],
  "metrics": [],
  "next": [],
  "warnings": []
}
```

Use `items` for records, `edges` for graph relationships, `diagnostics` for
build/test issues, and `metrics` for benchmark/log metrics. Use `next` to avoid
guessing follow-up commands.

## Core Commands

### `doctor`

Checks whether the toolchain can run.

```powershell
.\tools\agent\agent.ps1 doctor --format table
```

Use before debugging toolchain failures, SDK issues, or cache problems.

### `context`

Returns compact repo status, solution, changed files, projects, and recommended
next commands.

```powershell
.\tools\agent\agent.ps1 context --format table
.\tools\agent\agent.ps1 context --changed-only --format json
```

Use at the start of most tasks.

### `projects`

Returns solution/project metadata and dependency information.

```powershell
.\tools\agent\agent.ps1 projects --format table
.\tools\agent\agent.ps1 projects --changed-only --affected --format json
.\tools\agent\agent.ps1 projects --project STFU.Rendering --format json
```

Use before editing project references, package references, build configuration,
or cross-project code.

### `packages`

Lists direct and transitive package data.

```powershell
.\tools\agent\agent.ps1 packages --format table
.\tools\agent\agent.ps1 packages --package Microsoft.CodeAnalysis --format json
.\tools\agent\agent.ps1 packages --direct --project Agent.Cli --format json
```

Use before changing NuGet dependencies or diagnosing package conflicts.

### `symbols`

Finds C# symbols semantically, with syntax/cache fallback.

```powershell
.\tools\agent\agent.ps1 symbols --name NprLayerFrame --format json
.\tools\agent\agent.ps1 symbols --name CompositeLayer --kind method --cache warm --max 5 --format json
.\tools\agent\agent.ps1 symbols --name Render --project STFU.Rendering --format table
```

Useful filters:

```text
--name <pattern>
--kind type|method|property|field|enum|record|interface|ctor|event|operator
--project <name>
--cache warm
--refresh
```

Use this before `rg` for C# code.

### `refs`

Finds semantic references to symbols.

```powershell
.\tools\agent\agent.ps1 refs --symbol-id "M:Namespace.Type.Method()" --format json
.\tools\agent\agent.ps1 symbols --name RenderFrame --kind method --format json |
  .\tools\agent\agent.ps1 refs --from-stdin --max 20 --format json
```

Use this before editing a public method, shared type, pipeline step, or UI bridge
contract.

### `member`

Extracts the smallest useful C# member/type body.

```powershell
.\tools\agent\agent.ps1 member --symbol-id "M:Namespace.Type.Method()" --format markdown
.\tools\agent\agent.ps1 member --file src\aot\STFU.NPR\Pipeline\NprPipeline.cs --line 42 --context 8 --format markdown
```

Use this instead of opening full `.cs` files when a specific type/member is
enough.

### `graph`

Builds project graphs or semantic call graphs.

```powershell
.\tools\agent\agent.ps1 graph --kind project --format table
.\tools\agent\agent.ps1 graph --root "M:Namespace.Type.Method()" --kind call --format json
.\tools\agent\agent.ps1 symbols --name Tick --kind method --format json |
  .\tools\agent\agent.ps1 graph --from-stdin --kind call --format json
```

Use for dependency impact, caller/callee analysis, and architecture checks.

### `diff`

Returns structured git status and optional semantic hunk mapping.

```powershell
.\tools\agent\agent.ps1 diff --format table
.\tools\agent\agent.ps1 diff --semantic --format json
.\tools\agent\agent.ps1 diff --semantic --changed-only --format json
```

Use before reviews and before deciding which files to inspect.

### `build`

Runs `dotnet build` and emits parsed diagnostics.

```powershell
.\tools\agent\agent.ps1 build --format json
.\tools\agent\agent.ps1 build --configuration Release --format json
.\tools\agent\agent.ps1 build --project tools\agent\src\Agent.Cli\Agent.Cli.csproj --format json
```

Pipe diagnostics into `member`:

```powershell
.\tools\agent\agent.ps1 build --format json |
  .\tools\agent\agent.ps1 member --from-stdin --context 12 --format markdown
```

### `test`

Runs `dotnet test` and emits failing tests.

```powershell
.\tools\agent\agent.ps1 test --format json
.\tools\agent\agent.ps1 test --project tests\Some.Tests\Some.Tests.csproj --filter "FullyQualifiedName~SomeCase" --format json
.\tools\agent\agent.ps1 test --no-build --format json
```

Use focused filters first. Do not run broad test suites when a focused test is
available and sufficient.

### `run`

Runs an explicit command and records output under `.logs/agent`.

```powershell
.\tools\agent\agent.ps1 run --command "dotnet --info" --format json
.\tools\agent\agent.ps1 run --cwd src\runtime\STFU.App --command "dotnet run --configuration Release" --format json
```

Use for smoke runs and repeatable local command artifacts.

### `logs`

Inspects run/build/test/benchmark logs and artifacts.

```powershell
.\tools\agent\agent.ps1 logs --latest --max 10 --format table
.\tools\agent\agent.ps1 logs --query bench-fbx-mesh-render --format json
.\tools\agent\agent.ps1 logs --query error --tail 80 --format markdown
```

Use before rerunning expensive workflows if recent logs already answer the
question.

### `bench`

Runs configured or explicit benchmark commands and records metrics.

```powershell
.\tools\agent\agent.ps1 bench --name render-profiles --format json
.\tools\agent\agent.ps1 bench --command "dotnet run --project src/runtime/STFU.App/STFU.App.csproj --configuration Release -- --help" --format json
```

Core benchmark logic is domain-agnostic. STFU-specific benchmark workflows live
in presets and config.

### `bench-history`

Compares benchmark/log history.

```powershell
.\tools\agent\agent.ps1 bench-history --format table
.\tools\agent\agent.ps1 bench-history --benchmark fbx-mesh-render --metric frameMs --format json
.\tools\agent\agent.ps1 bench-history --regressions-only --format json
```

Use before performance work to identify whether the regression is real and which
run should be treated as baseline.

### `hotspots`

Finds static risk/perf patterns.

```powershell
.\tools\agent\agent.ps1 hotspots --format table
.\tools\agent\agent.ps1 hotspots --rules alloc-in-loop,linq-in-loop,sync-wait --rank risk --max 20 --format json
.\tools\agent\agent.ps1 diff --semantic --format json |
  .\tools\agent\agent.ps1 hotspots --from-stdin --rank risk --format json
```

Use for performance-sensitive rendering/import/NPR code, especially after a
semantic diff.

### `concat`

Builds a token-aware context bundle from prior tool output.

```powershell
.\tools\agent\agent.ps1 diff --semantic --format json |
  .\tools\agent\agent.ps1 concat --from-stdin --budget-tokens 8000 --format markdown
```

Use when handing context to another agent or preparing a compact investigation
bundle. Prefer `concat` over old full-repo `concat.txt`.

### `search`

Structured text fallback using `rg`.

```powershell
.\tools\agent\agent.ps1 search --pattern "--bench-render" --literal --format json
.\tools\agent\agent.ps1 search --pattern "Readback|Map|CopyResource" --type cs --max 40 --format json
```

Use for JSON, XML, PowerShell, docs, CLI flags, generated names, magic strings,
or when semantic C# tools cannot represent the query.

### `cache`

Inspects or clears `.agents/cache`.

```powershell
.\tools\agent\agent.ps1 cache status --format json
.\tools\agent\agent.ps1 cache clear --format json
```

Use `cache clear` only when stale results are suspected or the index format has
changed.

### `server`

Manages the background workspace/cache warmer.

```powershell
.\tools\agent\agent.ps1 server start --format json
.\tools\agent\agent.ps1 server status --format table
.\tools\agent\agent.ps1 server stop --format json
```

Normal fast path:

1. server starts or is started by the wrapper
2. server loads workspace and writes cache
3. commands read warm cache without blocking on IPC

Use `--use-server` only when deliberately testing direct daemon IPC.

### `preset`

Runs thin STFU-specific workflows built from core commands.

```powershell
.\tools\agent\agent.ps1 preset fbx-mesh-regression --format json
.\tools\agent\agent.ps1 preset npr-parity-audit --format json
.\tools\agent\agent.ps1 preset gpu-readback-audit --format json
.\tools\agent\agent.ps1 preset render-perf-release --format json
.\tools\agent\agent.ps1 preset allocation-sweep --format json
.\tools\agent\agent.ps1 preset aot-boundary-check --format json
```

Presets may know STFU terms. Core commands should stay domain-agnostic.

## Chaining Rules

Use JSON between tools:

```powershell
.\tools\agent\agent.ps1 symbols --name SomeMethod --kind method --format json |
  .\tools\agent\agent.ps1 refs --from-stdin --format json |
  .\tools\agent\agent.ps1 member --from-stdin --context 8 --format markdown
```

`--from-stdin` accepts:

- full `agent.tool.v1` envelopes
- JSON arrays of items
- NDJSON
- plain text lines as simple queries

Common mappings:

- `symbols` text input -> name query
- `refs` symbol item -> `symbolId`
- `member` diagnostic item -> enclosing member around `file:line`
- `member` reference item -> enclosing caller member
- `graph` symbol/member item -> graph root
- `concat` any item -> compact bundle

## Standard Recipes

### Repo orientation

```powershell
.\tools\agent\agent.ps1 context --format table
.\tools\agent\agent.ps1 projects --format table
```

### Find a symbol and read only its member

```powershell
.\tools\agent\agent.ps1 symbols --name BakeCombinedMesh --kind method --format json |
  .\tools\agent\agent.ps1 member --from-stdin --context 6 --format markdown
```

### Find callers and inspect caller bodies

```powershell
.\tools\agent\agent.ps1 symbols --name RenderFrame --kind method --format json |
  .\tools\agent\agent.ps1 refs --from-stdin --max 20 --format json |
  .\tools\agent\agent.ps1 member --from-stdin --context 8 --budget-lines 300 --format markdown
```

### Build failure to focused code

```powershell
.\tools\agent\agent.ps1 build --format json |
  .\tools\agent\agent.ps1 member --from-stdin --context 12 --format markdown
```

### Semantic code review

```powershell
.\tools\agent\agent.ps1 diff --semantic --format json |
  .\tools\agent\agent.ps1 refs --from-stdin --max 50 --format json |
  .\tools\agent\agent.ps1 hotspots --from-stdin --rank risk --max 20 --format json |
  .\tools\agent\agent.ps1 concat --from-stdin --budget-tokens 10000 --format markdown
```

### Project impact

```powershell
.\tools\agent\agent.ps1 projects --changed-only --affected --format json |
  .\tools\agent\agent.ps1 build --from-stdin --format json
```

### Benchmark regression

```powershell
.\tools\agent\agent.ps1 bench-history --benchmark fbx-mesh-render --regressions-only --format json |
  .\tools\agent\agent.ps1 diff --from-stdin --semantic --format json |
  .\tools\agent\agent.ps1 hotspots --from-stdin --rank hotness --max 25 --format json |
  .\tools\agent\agent.ps1 concat --from-stdin --budget-tokens 9000 --format markdown
```

### Text fallback to semantic context

```powershell
.\tools\agent\agent.ps1 search --pattern "Readback|CopyResource|Map" --type cs --format json |
  .\tools\agent\agent.ps1 member --from-stdin --context 8 --format markdown
```

## Performance Guidance

Fast path for repeated semantic work:

```powershell
.\tools\agent\agent.ps1 server start --format json
.\tools\agent\agent.ps1 symbols --name CompositeLayer --kind method --cache warm --format json
```

Rules:

- keep the server as a cache warmer, not a required dependency
- use `--cache warm` for repeated `symbols` queries
- use `--refresh` after changing symbols if results look stale
- use `cache status` to inspect cache files
- use `server stop` before debugging daemon behavior
- avoid `--use-server` unless testing direct IPC

If a semantic command feels slow, check whether it is loading Roslyn cold, doing
an uncached operation, or returning too much output. Narrow with `--project`,
`--kind`, `--max`, `--where`, or `--select`.

## Validation Policy

Default validation for code changes:

```powershell
.\tools\agent\agent.ps1 build --format json
```

Equivalent fallback:

```powershell
dotnet build STFU.slnx -v minimal
```

For pure docs changes, build is optional. For code changes, run the narrowest
validation that proves the touched surface:

- agent CLI changes: build `tools/agent/src/Agent.Cli/Agent.Cli.csproj`
- project/package changes: build affected project or solution
- shared runtime/NPR changes: build solution unless a narrower affected project
  proves the change
- tests: run focused tests first, broad tests only when needed
- UI/visual changes: build plus focused smoke/browser/screenshot when a local
  target is available
- performance work: use `bench`, `bench-history`, logs, and a focused build

Do not run broad formatting or analyzer cleanup across the repository unless
explicitly requested.

## General Behavior

- Make focused changes.
- Preserve existing style and ownership boundaries.
- Do not introduce abstractions without a concrete local payoff.
- Do not modify unrelated files.
- Do not revert user changes unless explicitly requested.
- Do not commit, push, install global tools, change CI/CD, or rewrite history
  unless explicitly requested.
- Prefer deterministic behavior in engine/NPR code.
- Keep comments sparse and useful.
- Keep final answers concise and include validation results.

## Git Workflow

Before editing, inspect the relevant state:

```powershell
git status --short
.\tools\agent\agent.ps1 diff --semantic --format table
```

When reviewing changes:

```powershell
git diff --stat
git diff -- path\to\file.cs
```

Never use destructive commands such as `git reset --hard` or `git checkout --`
unless the user explicitly asked for that exact operation.

## When Raw Shell Is Appropriate

Use raw shell commands when:

- the task is not C# semantic code navigation
- querying JSON, XML, PowerShell, docs, assets names, or CLI flags
- the toolchain is broken and you are diagnosing it
- a simple command directly answers the user

Preferred raw tools:

```powershell
rg --files
rg "pattern" path -n -C 2
git status --short
git diff --stat
dotnet build STFU.slnx -v minimal
```

Avoid chained noisy command output. For independent reads, run them in parallel
when the execution environment supports it.
