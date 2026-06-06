# Agent tools

Thin PowerShell entrypoints for low-token repository work. The router is:

```powershell
.\tools\agent\agent.ps1 <command> [flags]
```

PowerShell only routes. `tools/agent/src/Agent.Cli` owns the `agent.tool.v1` envelope, Roslyn/MSBuild loading, command execution, and formatting.

Core commands:

```text
doctor        toolchain/config/cache checks
context       compact repo summary
projects      solution and project metadata
packages      direct NuGet references
symbols       semantic C# symbol search with syntax fallback
refs          semantic references via Roslyn SymbolFinder
member        member extraction by symbolId or file:line
graph         project graph or semantic call graph
build         dotnet build with parsed diagnostics
test          dotnet test with basic failure extraction
run           run an explicit command and record .logs/agent output
diff          git status plus optional syntax semantic hunk mapping
hotspots      static risk/perf pattern scan
logs          run/log artifact browser
bench         command-backed benchmark runner
bench-history metric scan over log history
concat        token-aware context bundle from piped file/line items
server        background daemon status/start/stop
cache         inspect/clear cache files and SQLite index
search        structured rg fallback
```

Common flags:

```text
--format json|ndjson|table|markdown
--from-stdin
--max <n>
--context <n>
--solution <path>
--project <path|name>
--refresh
--cache warm
--use-server
--include <scope[,scope]|glob>
--profile <name>|all
```

Useful chains:

```powershell
.\tools\agent\agent.ps1 symbols --name CompositeLayer --kind method --format json |
  .\tools\agent\agent.ps1 refs --from-stdin --max 20 --format json

.\tools\agent\agent.ps1 symbols --name CompositeLayer --kind method --format json |
  .\tools\agent\agent.ps1 member --from-stdin --context 8 --format markdown

.\tools\agent\agent.ps1 diff --semantic --format json |
  .\tools\agent\agent.ps1 concat --from-stdin --budget-lines 300 --format markdown

.\tools\agent\agent.ps1 diff --semantic --format json |
  .\tools\agent\agent.ps1 concat --from-stdin --include tools --budget-lines 300 --format markdown

.\tools\agent\agent.ps1 symbols --name NprLayerFrame --cache warm --format json

.\tools\agent\agent.ps1 cache status --format json
```

Compatibility wrappers still work:

```powershell
.\tools\agent\context.ps1 --format json
.\tools\agent\search.ps1 --pattern NprLayerFrame --type cs
.\tools\agent\build.ps1 --project tools\agent\src\Agent.Cli\Agent.Cli.csproj
```

STFU presets live under `tools/agent/presets/stfu` and are intentionally thin orchestration scripts:

```powershell
.\tools\agent\agent.ps1 preset fbx-mesh-regression --format json
.\tools\agent\agent.ps1 preset npr-parity-audit --format json
.\tools\agent\agent.ps1 preset gpu-readback-audit --format json
```

Design rules:

- stdout is data; router build logs go to stderr
- output is capped by default
- JSON is the contract; table/markdown are views
- `search` is a fallback, not the semantic default
- `.agents/cache/index.sqlite` is the warm cache; `.agents/cache/*.jsonl` are readable sidecars
- `server start` launches a background `server-daemon` that preloads the workspace and warms cache; direct IPC is opt-in with `--use-server`
- `concat` uses config-driven profiles and include scopes; default STFU profile is `source`
- `alwaysExcludePatterns` in `agent.config.json` skips generated noise such as `artifacts/`, `logs/`, `test-results/`, `release/`, and old concat bundles
- `includeScopes` in `agent.config.json` defines optional context such as `tools`, `maquettes`, `third_party`, and `assets`
- STFU-specific workflows live in presets/config, not in core command logic
