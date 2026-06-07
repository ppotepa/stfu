# Hot Path Allocation Audit

## Guarded files

- `src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildInkFrameStep.cs`
- `src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildFaceIdVisibilityBufferStep.cs`
- `src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultClassifyEdgesToFragmentsStep.cs`
- `src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildPathsFromFragmentsStep.cs`
- `src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultSimplifyAndSortPathsStep.cs`
- `src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs`
- `src/aot/STFU.Rendering.Cpu/Rasterization/CpuToneRasterizer.cs`
- `src/runtime/STFU.Rendering.DirectX/Upload/DxStrokeInstanceBuilder.cs`

## Forbidden common operators

- `.ToArray(`
- `.ToList(`
- `.Select(`
- `.Where(`
- `.GroupBy(`
- `.OrderBy(`
- `.OrderByDescending(`
- `.ThenBy(`
- `.ThenByDescending(`

## Allowed exception

Use the exact marker only for reviewed non-frame-path cases:

```csharp
// HOTPATH-GUARD:ALLOW
```

## Validation

dotnet test tests/STFU.NPR.Parity.Tests/STFU.NPR.Parity.Tests.csproj -c Release
