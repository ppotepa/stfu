# FBX Import Architecture

STFU treats FBX as an import format, not as an engine-domain format.

The engine-facing model is:

```text
FBX
  -> STFU.Native.Fbx
  -> STFU.Import.Fbx
  -> ImportedAsset
  -> MeshData / future SkinnedMeshData / AnimationClip
  -> NPR pipeline
```

## Projects

- `src/aot/STFU.Animation`: AOT-friendly domain types for skeletons, skinning and animation clips.
- `src/runtime/STFU.Import`: runtime import abstraction for whole assets.
- `src/runtime/STFU.Import.Fbx`: C# FBX loader using source-generated `LibraryImport`.
- `src/native/STFU.Native.Fbx`: native C ABI wrapping `ufbx`.
- `third_party/ufbx`: vendored FBX backend source.

`STFU.Engine`, `STFU.NPR` and `STFU.Viewport` should not depend on FBX, `ufbx`, P/Invoke or native DLL names.

## Native Build

With clang available:

```powershell
New-Item -ItemType Directory -Force -Path artifacts\native\STFU.Native.Fbx | Out-Null
clang -std=c99 -O2 -shared -o artifacts\native\STFU.Native.Fbx\stfu_fbx.dll src\native\STFU.Native.Fbx\stfu_fbx.c third_party\ufbx\ufbx.c -Ithird_party\ufbx
Copy-Item -Force artifacts\native\STFU.Native.Fbx\stfu_fbx.dll src\runtime\STFU.App\bin\Debug\net10.0\stfu_fbx.dll
```

With CMake:

```powershell
cmake -S src/native/STFU.Native.Fbx -B artifacts/native/STFU.Native.Fbx -G Ninja
cmake --build artifacts/native/STFU.Native.Fbx
```

## Probe

```powershell
dotnet run --project src\runtime\STFU.App\STFU.App.csproj -- --probe-fbx assets\walking.fbx
dotnet run --project src\runtime\STFU.App\STFU.App.csproj -- --probe-fbx assets\walking.fbx 0 0.5
```

The optional arguments are:

- `animationIndex`
- `timeSeconds`

Current behavior bakes FBX meshes at the requested animation time into ordinary `MeshData`. It also materializes one scene-level `SkeletonData` and animation stack metadata as `AnimationClip` objects. This lets the current NPR pipeline consume animated assets without needing to understand skinning yet.

## Current Limits

- The loader materializes skeleton hierarchy and animation clip metadata.
- It does not yet materialize `SkinnedMeshData`, vertex skin weights or full animation keyframe tracks.
- It bakes mesh data through the native wrapper, which is enough for NPR projection/strokes but not enough for animation editing UI.

## Next Steps

1. Export skeleton hierarchy from `STFU.Native.Fbx` into `SkeletonData`.
2. Export skin weights into `ImportedSkinnedMesh`.
3. Export animation stacks into `AnimationClip`.
4. Add `AnimationSystem` that samples clips and bakes current poses.
5. Cache FBX topology so per-frame NPR recomputes only pose/projection-dependent data.
