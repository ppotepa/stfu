# Mesh/FBX Performance Results

## Current State

The `walking.fbx` mesh preview bottleneck was split into three independent costs:

- wireframe topology inflation from FBX raw corner/index vertices,
- CPU-side GPU mesh preparation through 2D stroke segments,
- per-frame FBX animation evaluation through `ufbx_evaluate_scene`.

The current optimized path uses welded/logical FBX topology, GPU-native mesh wireframe rendering, and an optional warmed FBX animation prebake cache.

## Validation Commands

```powershell
dotnet build .\STFU.slnx -v minimal
dotnet .\artifacts\bench-release\STFU.App.dll --smoke-fbx-abi assets\walking.fbx
dotnet .\artifacts\bench-release\STFU.App.dll --smoke-fbx-ui-load assets\walking.fbx
dotnet .\artifacts\bench-release\STFU.App.dll --smoke-gpu-readback 640 360
dotnet .\artifacts\bench-release\STFU.App.dll --verify-gpu-mesh-parity assets\walking.fbx 640 360 60 --animation off
dotnet .\artifacts\bench-release\STFU.App.dll --verify-gpu-mesh-parity assets\walking.fbx 640 360 60 --animation fixed-step --animation-cache-warmup 5
```

## Benchmark Matrix

Run these after a Release build to compare CPU, GPU-native, GPU-stroke fallback, and animation cache behavior:

```powershell
dotnet .\artifacts\bench-release\STFU.App.dll --bench-render-profiles assets\walking.fbx 640 360 120 mesh 20 --animation off --gpu-mesh-path native
dotnet .\artifacts\bench-release\STFU.App.dll --bench-render-profiles assets\walking.fbx 640 360 120 mesh 20 --animation off --gpu-mesh-path stroke
dotnet .\artifacts\bench-release\STFU.App.dll --bench-render-profiles assets\walking.fbx 800 600 120 mesh 20 --animation fixed-step --animation-cache-warmup 5 --gpu-mesh-path native
dotnet .\artifacts\bench-release\STFU.App.dll --bench-render-profiles assets\walking.fbx 1920 1080 120 mesh 20 --animation fixed-step --animation-cache-warmup 5 --gpu-mesh-path native
dotnet .\artifacts\bench-release\STFU.App.dll --bench-render-profiles assets\suzanne.obj 800 600 120 mesh 20 --animation off --gpu-mesh-path native
```

## Acceptance

- Full CPU render parity remains exact for CPU optimizer checks.
- GPU native mesh vs GPU stroke fallback is accepted with `PixelSurfaceDiff` channel tolerance `1`.
- `--gpu-mesh-path native` is the default and expected performance path.
- `--gpu-mesh-path stroke` is a debug/fallback path for visual parity and regression isolation.
- No optimization may reduce resolution, edge count through approximation, antialiasing quality, stroke style, layer order, or alpha blending semantics.

## GPU Mesh Parity Result

`--verify-gpu-mesh-parity assets\walking.fbx 640 360 3 --animation off` passes with exact pixel hashes after warmup:

```text
GPU mesh parity frame 2 ok: native=0x510BFF6C35FEEB29, stroke=0x510BFF6C35FEEB29,
nativePaths=27586, strokePaths=27586, nativeMesh=1.735ms, strokeMesh=6.518ms
```

The native mesh path now uses the same screen-space projection as the stroke fallback, while keeping an edge-buffer instanced shader path and avoiding stroke instance generation.

## Latest 640x360 Walking Results

```text
native, animation off:
cpu-gpu-direct avgTotal=1.83ms, 545.2 FPS, avgGpuMesh=1.09ms, avgGpuUpload=0.72ms
cpu-gpu-readback avgTotal=1.54ms, 649.2 FPS, avgGpuMesh=1.14ms, avgGpuUpload=0.06ms

stroke fallback, animation off:
cpu-gpu-direct avgTotal=8.44ms, 118.5 FPS, avgGpuMesh=6.77ms, avgGpuStrokeBuild=0.49ms, avgGpuUpload=1.16ms
cpu-gpu-readback avgTotal=6.18ms, 161.7 FPS, avgGpuMesh=5.45ms, avgGpuStrokeBuild=0.26ms

fixed-step animation with warm cache:
GPU mesh parity passes for 3 frames with exact matching hashes.
```
