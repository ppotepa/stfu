# AGENTS.md — STFU .NET agent instructions

## Purpose

This file defines how an LLM coding agent should work in the STFU repository.

The repository is a .NET 10 AOT/runtime hybrid for a small 3D/2D engine and NPR drawing pipeline. The default assumptions are optimized for C#, Roslyn-based tooling, PowerShell, MSBuild, NuGet, AOT-safe engine code, Avalonia runtime UI, and minimal validation.

The main goal is to reduce unnecessary context usage while keeping changes safe, reviewable, and easy to validate.

---

## Prime directive

Use the smallest reliable context needed to complete the task.

Prefer this order:

1. Semantic tools: Roslyn, LSP, MCP, IDE index, symbol search, reference search, diagnostics.
2. Project-aware tools: `dotnet`, MSBuild, NuGet, test runners, analyzers.
3. Structured search: `ast-grep` / `sg`, Roslynator, ReSharper CLI, architecture tests.
4. Text search: `rg`, `fd`, `Select-String`.
5. Full file reads only when smaller targeted reads are insufficient.

Do not scan or dump the whole repository unless the task explicitly requires it.

---

## STFU project map

Use this map before reading files:

- `STFU.slnx`: canonical solution.
- `src/aot/`: AOT-compatible engine libraries. Keep these explicit, deterministic, and reflection-light.
- `src/aot/STFU.Engine/`: engine builder, module registry, scene/entity basics.
- `src/aot/STFU.NPR/`: shared NPR abstractions, graph data, settings, styles, registry, pipeline interfaces.
- `src/aot/STFU.NPR.Preset.*`: style-only preset projects. These should not own pipeline construction.
- `src/aot/pipelines/STFU.NPR.Pipeline.*`: standalone NPR pipeline projects. These own pipeline construction and may expose built-in presets.
- `src/runtime/STFU.App/`: console host that starts the UI and logs.
- `src/runtime/STFU.UI/`: Avalonia UI and viewport host. Runtime-only code may use reflection and UI libraries.
- `src/runtime/STFU.Import*/`: runtime import/external tooling.
- `src/native/`: native interop code.
- `assets/`: large local models/assets. Avoid reading unless the task is asset/import/visual validation.
- `docs/`: theory, architecture, and planning documents.
- `maquettes/`: HTML/CSS/JS UI prototypes.
- `third_party/`: vendored external code. Avoid unless the task explicitly targets it.

---

## STFU context-saving rules

Avoid reading these unless the task explicitly requires them:

- `concat.txt`
- `concat.zip`
- large files under `assets/`
- `third_party/ufbx/`, except native FBX/import tasks
- `maquettes/`, except UI/prototype tasks
- `docs/NPR_SUPPLEMENT_IMPL.md`, except architecture/theory tasks
- generated `bin/` and `obj/` trees

For normal implementation tasks, start from:

```powershell
git status --short
rg "RelevantSymbol|RelevantType|RelevantCommand" src -n
dotnet build STFU.slnx
```

Read only direct callers/callees and nearby patterns. Do not read long theory docs to solve narrow code tasks.

---

## AOT and runtime boundaries

`src/aot` projects must remain AOT-friendly:

- Avoid reflection-based discovery.
- Avoid dynamic assembly scanning.
- Avoid runtime code generation.
- Prefer explicit registration, static providers, source-known modules, and deterministic data flow.
- Do not reference Avalonia from `src/aot`.
- Do not place runtime plugin loading, native importer orchestration, or UI-specific code in `src/aot`.

`src/runtime` projects may use:

- Avalonia
- reflection
- runtime plugin loading
- external/native import tooling
- console/UI process coordination

When in doubt, keep abstractions and data contracts in `src/aot`, and keep discovery, UI, plugins, import orchestration, and process-specific behavior in `src/runtime`.

---

## NPR architecture invariants

Preserve this split:

- `INprPreset` owns metadata, `PipelineId`, settings, grammar, and style set.
- `INprPreset` must not create pipelines.
- `INprPipelineProvider` owns `PipelineId`, `CreatePipeline()`, and optional `CreateBuiltInPresets()`.
- `NprModule` registers pipeline providers first, then provider built-in presets, then additional presets.
- `ActiveNprPresetState` resolves the active pipeline only through `preset.PipelineId`.
- If a preset points to an unregistered pipeline, fail loudly with a clear exception.

Current placement rules:

- Shared NPR abstractions: `src/aot/STFU.NPR/`.
- Pipeline-specific implementation: `src/aot/pipelines/STFU.NPR.Pipeline.<Name>/`.
- Style-only preset packs: `src/aot/STFU.NPR.Preset.<Name>/`.
- Avalonia viewport/UI behavior: `src/runtime/STFU.UI/`.

Do not resurrect deleted old NPR step implementations unless explicitly requested. New pipeline work should be built inside a pipeline project first; promote code into `STFU.NPR` only when it becomes a stable shared abstraction.

---

## STFU validation policy

Default validation for code changes:

```powershell
dotnet build STFU.slnx
```

Do not add broad tests unless requested. Add minimal tests only for stable public behavior, regressions, serialization contracts, or high-risk shared logic.

For UI/NPR visual work, prefer:

- build
- focused smoke run
- concise log inspection
- screenshot/browser inspection only when the task is visual/frontend and a local target is available

Do not run broad formatting or analyzer cleanup across the repository unless explicitly requested.

---

## STFU agent task profiles

Use the smallest profile that fits the task:

- Architecture agent: read `AGENTS.MD`, `README.md`, relevant local docs only if requested, then targeted files.
- Implementation agent: read changed symbols and direct callers only. Do not read `docs/` unless the task asks for architecture/theory.
- Debug agent: start from the exact error log, stack trace, file, and line. Inspect direct dependencies before broader search.
- NPR pipeline agent: preserve preset/pipeline separation. Add new steps inside `src/aot/pipelines/STFU.NPR.Pipeline.<Name>/` unless a shared abstraction is needed.
- UI agent: use `maquettes/` only when UI behavior/layout is the task. Keep Avalonia code in `src/runtime/STFU.UI/`.
- Import/native agent: inspect `docs/FBX_IMPORT.md`, `src/runtime/STFU.Import*`, `src/native/`, and `third_party/ufbx/` only when import/native behavior is the task.

Local instruction files:

```text
src/aot/AGENTS.MD
src/aot/STFU.NPR/AGENTS.MD
src/aot/pipelines/AGENTS.MD
src/runtime/STFU.UI/AGENTS.MD
docs/AGENTS.MD
maquettes/AGENTS.MD
assets/AGENTS.MD
```

Read local files only when working in or near that subtree. They are intentionally short and should replace broad repository scanning.

---

## Low-token tool recipes

Prefer these recipes before opening files directly.

### Repo orientation

```powershell
.\tools\agent\context.ps1
```

Manual fallback:

```powershell
git status --short
git diff --stat
rg --files src docs maquettes --glob '!**/bin/**' --glob '!**/obj/**' | Select-Object -First 160
```

### Focused search

```powershell
.\tools\agent\search.ps1 "ActiveNprPresetState|INprPreset|PipelineId"
```

Manual fallback:

```powershell
rg "ActiveNprPresetState|INprPreset|PipelineId" src docs -n -C 2 `
  --glob '!**/bin/**' --glob '!**/obj/**' `
  --glob '!assets/**' --glob '!third_party/**' |
  Select-Object -First 120
```

### Diff review

```powershell
.\tools\agent\diff.ps1
```

Inspect specific files after `git diff --stat`; do not dump all diffs by default.

### Build

```powershell
.\tools\agent\build.ps1
```

This runs the canonical solution build with minimal verbosity. For pure docs changes, build is optional.

### Project graph

```powershell
.\tools\agent\projects.ps1
```

Use this before changing project references, package references, or AOT/runtime boundaries.

### Large docs

Do not read `docs/NPR_SUPPLEMENT_IMPL.md` directly for implementation tasks. Prefer:

- `docs/AGENT_CONTEXT.md`
- `docs/NPR_ARCHITECTURE_BRIEF.md`
- `docs/UI_ARCHITECTURE_BRIEF.md`
- `docs/IMPORT_ARCHITECTURE_BRIEF.md`

Open the long document only when the task explicitly asks for theory, architecture, or detailed research.

---

## General behavior

- Make the smallest safe change.
- Preserve existing architecture, naming, formatting, test style, and public contracts.
- Do not perform broad refactors together with feature/bug work unless explicitly requested.
- Do not commit, push, install global tools, change CI/CD, or rewrite history unless explicitly requested.
- Do not delete or overwrite user changes.
- Do not expose secrets, tokens, credentials, connection strings, private keys, dumps, or production data.
- Prefer deterministic, non-interactive commands.
- Keep command output short and filtered.
- Explain assumptions when they affect correctness.

---

## Context budget rules

Default limits:

- Search results: maximum 80 lines.
- File preview: maximum 200 lines.
- Diff preview: start with `git diff --stat`, then inspect specific files.
- Build/test output: use minimal verbosity first.
- Logs: show only relevant errors and nearby context.

Avoid reading these unless required:

- `.git/`
- `.vs/`
- `.idea/`
- `bin/`
- `obj/`
- `node_modules/`
- `TestResults/`
- `coverage/`
- `artifacts/`
- generated files
- minified files
- lock files, unless dependency resolution is the task
- large snapshots/golden files, unless test failure requires them

When reading a file, prefer a focused line range.

---

## Tool availability

Do not assume every tool is installed. Detect and degrade gracefully.

Useful discovery command:

```powershell
$tools = @(
  'dotnet', 'git', 'rg', 'fd', 'jq', 'yq', 'bat', 'sg',
  'roslynator', 'jb', 'ilspycmd', 'gh', 'semgrep',
  'tokei', 'hyperfine', 'dotnet-counters', 'dotnet-trace',
  'dotnet-dump', 'dotnet-gcdump', 'dotnet-stack'
)

$tools | ForEach-Object {
  [pscustomobject]@{
    Tool = $_
    Found = [bool](Get-Command $_ -ErrorAction SilentlyContinue)
  }
} | Format-Table -AutoSize
```

If a tool is missing:

1. Use the next best available tool.
2. Do not install global tools without explicit approval.
3. Prefer local .NET tool manifests when a repo needs repeatable tooling:

```powershell
dotnet new tool-manifest
# then install tools locally, for example:
# dotnet tool install roslynator.dotnet.cli
# dotnet tool restore
```

---

## Semantic navigation first

When available, use IDE/MCP/LSP/Roslyn tools before shell search.

Preferred semantic capabilities:

- find symbol
- find references
- go to definition
- go to implementation
- type hierarchy
- call hierarchy
- diagnostics
- code actions
- safe rename
- refactor preview
- test discovery
- dependency graph
- project graph
- NuGet package graph
- MSBuild/binary log analysis

Examples of acceptable semantic backends:

- Roslyn / Microsoft.CodeAnalysis
- C# LSP
- C# Dev Kit / Visual Studio / Rider index
- OmniSharp
- Serena MCP
- JetBrains MCP
- GliderMCP or other Roslyn-aware MCP servers
- Sourcegraph-style code search/index
- NuGet MCP
- MSBuild binlog MCP/analyzer

Rules:

- Do not approximate symbol semantics with regex if a semantic tool is available.
- Use text search only as fallback or to supplement semantic results.
- If a semantic result is broad, narrow it by project, namespace, type, member, or test name.
- Prefer reference/call hierarchy before editing shared APIs.

---

## Initial repository scan

Start with a small map. Do not recursively inspect everything.

```powershell
git status --short
```

Find repository root:

```powershell
git rev-parse --show-toplevel
```

Find solutions and projects with `fd`:

```powershell
fd -e sln -e slnx -e slnf .
fd -e csproj -e fsproj -e vbproj .
```

PowerShell fallback:

```powershell
Get-ChildItem -Recurse -File -Include *.sln,*.slnx,*.slnf,*.csproj,*.fsproj,*.vbproj |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs|\.idea)\\' } |
  Select-Object -First 120 FullName
```

Find important repo-level files:

```powershell
fd "^(global\.json|Directory\.Build\.(props|targets)|Directory\.Packages\.props|NuGet\.config|nuget\.config|\.editorconfig)$" .
```

If multiple solutions exist, choose the narrowest solution/project relevant to the task. Ask only when ambiguity blocks a safe change.

---

## PowerShell usage rules

Prefer PowerShell 7+ (`pwsh`) when available.

Use output caps:

```powershell
<command> | Select-Object -First 80
```

Use `Select-String` with context instead of full file reads:

```powershell
Select-String -Path .\src\**\*.cs -Pattern "SomeSymbol" -Context 3,8 |
  Select-Object -First 40
```

Preview a line range without reading the whole file:

```powershell
Get-Content .\path\to\file.cs | Select-Object -Skip 40 -First 120
```

Prefer native tools for speed when available:

```powershell
rg "SomeSymbol" src tests -n -C 3 --glob '!bin/**' --glob '!obj/**'
fd "\.cs$" src tests
```

Avoid interactive pagers. Disable paging/color when output is consumed by the agent.

---

## Generic CLI toolbelt

Use these tools when available.

### File discovery

```powershell
fd "\.cs$" src tests
fd "Controller|Endpoint|Handler|Service|Repository" src tests
fd -e csproj -e props -e targets -e json -e yml -e yaml -e config .
```

Fallback:

```powershell
Get-ChildItem -Recurse -File -Include *.cs,*.csproj,*.props,*.targets,*.json,*.yml,*.yaml,*.config |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs|\.idea)\\' } |
  Select-Object -First 120 FullName
```

### Text search

```powershell
rg "class OrderService|interface IOrderService|HandleAsync" src tests -n -C 3
rg "IOrderRepository" src tests -n | Select-Object -First 60
rg "TODO|HACK|BUG|FIXME" src tests -n | Select-Object -First 80
```

### Structural search

Use `sg` / `ast-grep` for syntax-aware search when plain text is too noisy.

```powershell
sg -p 'await $EXPR.ToListAsync()' -l csharp src
sg -p 'public class $NAME { $$$ }' -l csharp src
sg -p 'throw new NotImplementedException()' -l csharp src tests
```

### JSON/YAML/XML filtering

Use `jq` / `yq` instead of printing full config or CLI JSON.

```powershell
dotnet package list --include-transitive --format json | jq '.projects[] | {path, frameworks}'
yq '.services' docker-compose.yml
```

### File preview

```powershell
bat .\src\Orders\OrderService.cs --line-range 40:140 --paging=never --style=plain
```

Fallback:

```powershell
Get-Content .\src\Orders\OrderService.cs | Select-Object -Skip 39 -First 100
```

---

## .NET solution/project discovery

Identify SDK and workload context only when relevant:

```powershell
dotnet --info
```

For shorter checks:

```powershell
dotnet --version
dotnet workload list
```

List solution projects:

```powershell
dotnet sln list
```

If a solution path is needed, set it explicitly:

```powershell
$Solution = (fd -e sln -e slnx . | Select-Object -First 1)
```

If there is no solution, work with the narrowest affected project:

```powershell
$Project = (fd -e csproj . | Select-Object -First 1)
```

Do not create a solution or move projects unless explicitly requested.

---

## .NET build workflow

Default order:

```powershell
dotnet restore $Solution
dotnet build $Solution --no-restore -v minimal
```

If no solution exists:

```powershell
dotnet restore $Project
dotnet build $Project --no-restore -v minimal
```

For CI-like verification, inspect repo scripts first:

```powershell
fd "build|test|ci|pipeline|workflow" .github build eng scripts .
```

Prefer existing repo scripts over inventing new commands.

---

## MSBuild diagnostics

For normal failures, use minimal console output first.

```powershell
dotnet build $Solution -v minimal
```

For project/restore/target/property issues, generate a binary log instead of dumping verbose MSBuild output:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
dotnet build $Solution -bl:artifacts/build.binlog -v minimal
```

Rules:

- Do not paste huge MSBuild logs.
- Prefer `.binlog` for deep build diagnostics.
- Inspect `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`, and `NuGet.config` before changing build behavior.
- Do not change MSBuild properties globally unless the effect is understood across all projects.

---

## Tests

Prefer targeted tests first.

Find test projects:

```powershell
fd "Tests?\.csproj$|\.Tests\.csproj$" .
rg "Microsoft.NET.Test.Sdk|xunit|NUnit|MSTest" . -n --glob '*.csproj'
```

Run one test class or method:

```powershell
dotnet test $Solution --no-build --filter "FullyQualifiedName~SomeTestClass" --logger "console;verbosity=minimal"
```

Run affected test project:

```powershell
dotnet test .\tests\SomeProject.Tests\SomeProject.Tests.csproj --logger "console;verbosity=minimal"
```

Run all tests only when necessary:

```powershell
dotnet test $Solution --no-build --logger "console;verbosity=minimal"
```

Testing rules:

- Add or update tests near existing tests when changing production behavior.
- Follow the test framework already used by the affected project.
- Prefer stable unit tests over broad integration tests unless behavior requires integration coverage.
- Do not update snapshots/golden files without confirming the behavior change is intentional.
- If tests are slow or require services, identify targeted alternatives first.

---

## Coverage

Use coverage only when relevant to the task or repo convention.

```powershell
dotnet test $Solution --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal"
```

If ReportGenerator is available:

```powershell
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"artifacts/coverage-report"
```

Do not add coverage tooling unless the task requires it.

---

## Formatting and analyzers

Follow `.editorconfig` and existing style.

Check formatting without changing files:

```powershell
dotnet format $Solution --verify-no-changes --verbosity minimal
```

Format only affected files when possible:

```powershell
dotnet format $Solution --include path/to/file.cs --verbosity minimal
```

Run analyzer formatting/checks when relevant:

```powershell
dotnet format analyzers $Solution --verify-no-changes --severity warn --verbosity minimal
```

Do not run broad auto-formatting on the entire repository unless explicitly requested or already required by CI.

---

## Roslyn/Roslynator workflow

Use Roslyn-based tooling for C# semantics, diagnostics, and safe refactoring.

If Roslynator is available:

```powershell
roslynator analyze $Solution --severity-level warning
```

Rules:

- Build first when semantic analysis needs compiled context.
- Treat analyzer output as guidance unless the repo enforces it in CI.
- Do not mass-apply fixes across unrelated code.
- Prefer local `.editorconfig` severity rules over ad-hoc style preferences.
- Do not introduce new analyzer packages without checking existing analyzer setup.

---

## ReSharper CLI workflow

If JetBrains ReSharper Global Tools are available, use them for optional deeper inspection.

Inspect code:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
jb inspectcode $Solution --output=artifacts/inspectcode.xml --format=Xml
```

Cleanup code only when explicitly requested or when matching repo convention:

```powershell
jb cleanupcode $Solution
```

Rules:

- Do not mix ReSharper cleanup with functional changes unless requested.
- Prefer inspection reports over large console output.
- Keep generated reports under `artifacts/` or another ignored output folder.

---

## Dependencies and NuGet

Before adding or updating packages:

1. Check whether the package already exists elsewhere in the repo.
2. Check whether central package management is used.
3. Check target frameworks and compatibility.
4. Prefer built-in .NET/BCL APIs when sufficient.
5. Keep versions centralized when `Directory.Packages.props` exists.
6. Run restore and build after dependency changes.

Find package references:

```powershell
rg "PackageReference|PackageVersion" . -n --glob '*.csproj' --glob '*.props' --glob '*.targets'
```

List packages on .NET 10+:

```powershell
dotnet package list $Solution --include-transitive --format json
```

Fallback for older SDKs:

```powershell
dotnet list $Solution package --include-transitive --format json
```

Check vulnerable packages:

```powershell
dotnet package list $Solution --vulnerable --include-transitive
```

Fallback for older SDKs:

```powershell
dotnet list $Solution package --vulnerable --include-transitive
```

Check outdated packages only when dependency updates are in scope:

```powershell
dotnet package list $Solution --outdated
```

Do not update packages opportunistically.

---

## Security checks

Use security tooling only when relevant to the task or CI convention.

Package vulnerabilities:

```powershell
dotnet package list $Solution --vulnerable --include-transitive
```

Semgrep, if available:

```powershell
semgrep scan --config auto --json --output artifacts/semgrep.json
```

GitHub context, if `gh` is available and authenticated:

```powershell
gh pr view --json title,state,files,checks
```

Rules:

- Never print secrets.
- Do not weaken authentication, authorization, CORS, CSRF, validation, crypto, or logging safety without explicit justification.
- Do not add secret values to appsettings, launch profiles, test fixtures, or docs.
- Use user-secrets, environment variables, vault integration, or existing repo conventions.

---

## Runtime diagnostics

Use runtime diagnostic tools only when the task is about performance, memory, hangs, exceptions, production-like behavior, or a running process.

List .NET processes:

```powershell
dotnet-counters ps
```

Monitor counters:

```powershell
dotnet-counters monitor -p <PID> System.Runtime
```

Collect trace:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
dotnet-trace collect -p <PID> -o artifacts/trace.nettrace
```

Collect dump:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
dotnet-dump collect -p <PID> -o artifacts/process.dmp
```

Inspect stacks:

```powershell
dotnet-stack report -p <PID>
```

Rules:

- Do not collect dumps/traces from production or user data processes without explicit approval.
- Treat dumps and traces as sensitive.
- Store diagnostic artifacts in ignored folders such as `artifacts/`.

---

## Decompilation and external APIs

Use `ilspycmd` only when source is unavailable and behavior/API shape must be verified.

```powershell
ilspycmd path/to/library.dll -t Namespace.TypeName
```

Rules:

- Prefer official source/docs when available.
- Do not paste large decompiled files.
- Use decompilation only to answer narrow compatibility questions.

---

## Database and migrations

Before modifying persistence:

1. Locate the DbContext or data access abstraction.
2. Inspect existing migration naming conventions.
3. Inspect transaction/unit-of-work conventions.
4. Inspect integration test setup.
5. Confirm whether migrations are part of the requested task.

Find EF Core usage:

```powershell
rg "DbContext|DbSet<|OnModelCreating|IEntityTypeConfiguration|HasOne|HasMany|OwnsOne" src tests -n -C 2
```

If `dotnet ef` is available:

```powershell
dotnet ef dbcontext list
```

Rules:

- Do not generate migrations unless explicitly required.
- If migrations are generated, include the model change, migration files, and relevant tests.
- Do not change production connection strings.
- Do not run destructive database commands unless explicitly requested.

---

## Architecture and design

Before changing architecture-sensitive code, inspect nearby patterns.

Check existing conventions for:

- dependency injection
- middleware/pipeline setup
- configuration binding
- validation
- error/result handling
- logging
- authorization
- transactions/unit of work
- caching
- background jobs
- messaging/events
- serialization
- API versioning
- test setup

Rules:

- Do not introduce a new architectural pattern when an equivalent already exists.
- Keep changes within the affected layer unless cross-layer updates are required.
- If public contracts change, update callers, tests, docs, and serialization expectations.
- Prefer explicit, boring, maintainable code over clever abstractions.

---

## ASP.NET / API rules

When modifying web/API code:

- Preserve existing controller/minimal API style.
- Preserve existing response/result conventions.
- Preserve validation and authorization behavior.
- Do not broaden CORS policy unless explicitly required.
- Do not expose internal exception details to clients.
- Use structured logging without secrets or sensitive payloads.
- Update OpenAPI/Swagger annotations only when the public API changes.
- Add or update integration tests when routing, auth, serialization, or middleware behavior changes.

Useful searches:

```powershell
rg "MapGet|MapPost|MapPut|MapDelete|ControllerBase|Authorize|AllowAnonymous|ProblemDetails|IEndpointRouteBuilder" src tests -n -C 2
```

---

## Async, cancellation, and reliability

Follow existing project style.

Default preferences:

- Use async APIs for I/O.
- Propagate `CancellationToken` where existing patterns support it.
- Do not block async code with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` unless unavoidable and justified.
- Avoid fire-and-forget tasks unless the project has a safe background execution pattern.
- Preserve retry, timeout, idempotency, and transaction behavior.

---

## Logging and errors

Follow existing error handling conventions.

Prefer:

- structured logging
- meaningful exception types
- preserving stack traces
- explicit validation errors
- existing Result/ProblemDetails/error-envelope patterns

Avoid:

- swallowing exceptions silently
- logging secrets or full sensitive payloads
- converting all errors to generic exceptions
- changing public error contracts accidentally

---

## Configuration

Before changing config:

1. Check `appsettings*.json`, environment variable naming, options classes, and validation.
2. Check `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>` usage.
3. Check deployment/CI conventions.

Useful search:

```powershell
rg "IOptions<|IOptionsSnapshot<|IOptionsMonitor<|Configure<|GetSection\(|Bind\(" src tests -n -C 2
```

Rules:

- Do not hardcode environment-specific values.
- Do not commit secrets.
- Preserve existing naming and binding conventions.
- Add validation for required options when the project already follows that pattern.

---

## Generated code

Do not edit generated files unless the task explicitly targets generated output.

Common generated indicators:

- `// <auto-generated />`
- `.g.cs`
- `.Designer.cs`
- generated clients
- generated EF migrations snapshots
- generated OpenAPI clients

Instead, modify the source template, schema, model, generator config, or source definition.

---

## Git workflow

Inspect status before editing:

```powershell
git status --short
```

Inspect changed files before finalizing:

```powershell
git diff --stat
git diff -- path/to/file.cs
```

Rules:

- Do not revert unrelated user changes.
- Do not commit unless explicitly requested.
- Do not amend, rebase, reset, or force-push unless explicitly requested.
- Keep diffs focused.
- Mention uncommitted unrelated changes if they affect validation.

---

## Review workflow

Before final response, check:

```powershell
git status --short
git diff --stat
```

For code changes, validation preference:

1. targeted tests
2. affected project tests
3. build
4. full solution tests only when warranted
5. formatting/analyzers if repo convention requires them

If validation cannot be run, state why.

---

## Final response format

Keep the final response short and factual.

Include:

- changed files
- what changed
- validation commands run
- commands not run and why
- risks/assumptions, if any

Do not include huge logs. Summarize errors with relevant lines only.

---

## Optional local AGENTS.md files

For large repositories, add smaller `AGENTS.md` files close to modules.

Examples:

```text
src/Payments/AGENTS.md
src/Identity/AGENTS.md
tests/AGENTS.md
```

Local files should contain only module-specific rules:

- domain vocabulary
- invariants
- local architecture
- test commands
- migration rules
- external service constraints
- files that should not be edited

Root `AGENTS.md` should stay generic and operational.

---

## Recommended agent tool profile

Best minimal profile:

```text
Roslyn/LSP/MCP semantic navigation
.NET SDK CLI
MSBuild binlog support
rg
fd
jq
yq
bat
ast-grep / sg
```

Strong .NET profile:

```text
Roslyn/LSP/MCP semantic navigation
.NET SDK CLI
Roslynator CLI
ReSharper Global Tools
MSBuild Structured Log / binlog analyzer
NuGet MCP or package graph tool
ILSpy / ilspycmd
.NET diagnostics tools
rg/fd/jq/yq/bat/sg
```

Enterprise profile:

```text
IDE index: Visual Studio or Rider
MCP: Roslyn/Serena/JetBrains/Sourcegraph/NuGet/MSBuild-binlog
Static analysis: Roslyn analyzers, ReSharper InspectCode, Semgrep, CodeQL, NDepend if available
Architecture tests: ArchUnitNET or NetArchTest when present
Observability: dotnet-counters, dotnet-trace, dotnet-dump, dotnet-stack
```

Use what is already available. Do not add tooling unless the task or repository requires it.
