# Agent tools

Small PowerShell wrappers for low-token repository work.

Use these before broad file reads:

```powershell
.\tools\agent\context.ps1
.\tools\agent\search.ps1 "SomeSymbol|SomeType"
.\tools\agent\diff.ps1
.\tools\agent\projects.ps1
.\tools\agent\build.ps1
```

Design rules:

- keep output capped
- avoid `assets`, `third_party`, `bin`, `obj`, and long theory docs by default
- prefer focused search over full file reads
- prefer `dotnet build STFU.slnx -v minimal` for validation

