# AOT agent instructions

This subtree contains NativeAOT-friendly engine libraries.

Rules:

- Keep code deterministic and explicit.
- Avoid reflection-based discovery, dynamic assembly scanning, runtime code generation, and UI dependencies.
- Prefer module registration through explicit `IEngineModule` implementations.
- Keep runtime-only behavior in `src/runtime`.
- Do not reference Avalonia from AOT projects.
- Do not add broad package dependencies without checking AOT compatibility.

Default validation:

```powershell
dotnet build STFU.slnx -v minimal
```

For narrow changes, inspect only direct callers and affected project references first.
