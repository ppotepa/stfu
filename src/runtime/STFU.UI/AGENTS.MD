# STFU.UI agent instructions

This project contains Avalonia UI and viewport host code.

Rules:

- UI code may use Avalonia and runtime-only facilities.
- Keep engine/NPR abstractions in `src/aot`; do not move UI concepts into AOT projects.
- Use `maquettes/` only when the task asks for target layout, UX, or prototype parity.
- Preserve console-host logging behavior; `STFU.App` starts UI and should remain useful for logs.
- For viewport/NPR rendering issues, start from `MainWindow`, `EngineViewportControl`, active preset state, and exact log lines.

Useful focused searches:

```powershell
.\tools\agent\search.ps1 "ApplyPreset|RenderMode|CreateNprFrame|DrawFrame|EngineViewportControl" src/runtime/STFU.UI src/aot/STFU.Viewport src/aot/STFU.NPR
```

Default validation:

```powershell
dotnet build STFU.slnx -v minimal
```
