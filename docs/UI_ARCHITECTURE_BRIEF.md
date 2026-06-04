# UI architecture brief

Read this for Avalonia/viewport tasks.

## Runtime shape

- `src/runtime/STFU.App`: console host. It starts the UI and should remain useful for logs.
- `src/runtime/STFU.UI`: Avalonia app, main window, viewport rendering, input handling.
- `src/aot/STFU.Viewport`: AOT-safe viewport-facing module/services.
- `src/aot/STFU.Camera`: camera state and commands.
- `src/aot/STFU.NPR`: NPR state, presets, graph, and render output contracts.

## Important UI behavior

- The app should start from the console host and keep logging visible.
- The viewport should support mesh and NPR render modes.
- Preset shortcuts are handled in UI/runtime, while preset definitions live in NPR projects.
- Orbit/pan/FOV input should update camera state through existing command patterns where possible.

## Maquettes

`maquettes/` contains target prototypes. Use them only when layout or visual behavior is the task.

Do not copy prototype architecture directly. Translate visual/layout behavior into Avalonia and keep engine logic in AOT projects.

## Validation

Default:

```powershell
dotnet build STFU.slnx -v minimal
```

For visual changes, prefer a focused smoke run and concise log/screenshot inspection.
