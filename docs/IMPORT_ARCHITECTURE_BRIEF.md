# Import architecture brief

Read this for OBJ/FBX/GLB/import tasks.

## Current shape

- `src/aot/STFU.Mesh`: mesh data and mesh-domain services.
- `src/aot/STFU.Mesh.IO`: AOT-safe mesh IO abstractions/loaders.
- `src/aot/STFU.Assets`: asset registry and handles.
- `src/runtime/STFU.Import`: runtime import helpers.
- `src/runtime/STFU.Import.Fbx`: FBX runtime importer integration.
- `src/native`: native interop.
- `third_party/ufbx`: vendored native FBX-related code. Avoid unless task targets native import.

## Boundary rule

AOT projects should expose stable data/contracts and deterministic loaders where practical. Runtime projects may orchestrate external libraries, native interop, reflection, or plugin loading.

## Asset rules

Do not open large assets by default. Prefer:

```powershell
Get-ChildItem assets -File | Select-Object Name, Length, LastWriteTime
Get-ChildItem assets -Recurse -File -Include *.obj,*.fbx,*.glb,*.gltf,*.amc | Select-Object FullName, Length
```

For importer bugs, start from:

- exact file path
- extension
- file size
- loader selected
- log/exception
- mesh vertex/triangle counts

## Validation

Default:

```powershell
dotnet build STFU.slnx -v minimal
```

For importer behavior, use a small asset first when available.
