# STFU.Native.Fbx

Native FBX adapter used by `STFU.Import.Fbx`.

The exported ABI is intentionally small and C-shaped:

- `stfu_fbx_load`
- `stfu_fbx_get_scene_info`
- `stfu_fbx_bake_mesh_at_time`
- `stfu_fbx_free_mesh_buffer`
- `stfu_fbx_free`

`STFU.Import.Fbx` calls these entry points through source-generated .NET `LibraryImport`.

Build example:

```powershell
cmake -S src/native/STFU.Native.Fbx -B artifacts/native/STFU.Native.Fbx -G Ninja
cmake --build artifacts/native/STFU.Native.Fbx
```

Copy the resulting `stfu_fbx.dll` next to the runtime executable before loading `.fbx` files from the app.
