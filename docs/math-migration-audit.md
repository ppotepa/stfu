# Math Migration Audit

Date: 2026-06-06

Source CSV: `C:\Users\ppotepa\.codex\attachments\abdf51d5-476b-4a10-bfb1-7001649b206b\pasted-text.txt`

## Current Status

- CSV rows reviewed by script: 400.
- Target `STFU.Common.Math` API classes referenced by the CSV: 20.
- Missing target API class files: 0.
- Raw `Math.`, `MathF.`, and `global::System.Math` usages outside `src/aot/STFU.Common/Math`: 0 by repository scan.
- Release app build: passing.
- Full solution tests: passing, 82/82.
- NPR parity tests: passing, 7/7.

## Target API Files Present

- `AnimationSamplingMath`
- `BufferSizingMath`
- `CameraMath`
- `ColorMath`
- `DiffMath`
- `GpuPackingMath`
- `HashMath`
- `MeshTopologyMath`
- `NoiseMath`
- `NumericMath`
- `PathMath`
- `ProjectionMath`
- `RangeMath`
- `RasterMath`
- `SignatureMath`
- `SizeMath`
- `StrokeMath`
- `Transform3D`
- `TransformMath`
- `VisibilitySamplingMath`

## Verified Commands

```powershell
dotnet build src\runtime\STFU.App\STFU.App.csproj --nologo -v:minimal -c Release -m:1
dotnet test STFU.slnx --nologo -c Release -v:minimal -m:1
dotnet test tests\STFU.NPR.Parity.Tests\STFU.NPR.Parity.Tests.csproj --nologo -c Release -v:minimal --no-restore -m:1
rg -n -P "\b(?:MathF|Math)\.|global::System\.Math" src\aot src\runtime -g "*.cs" -g "!src/aot/STFU.Common/Math/*.cs"
```

## Remaining Non-Math Domain Helpers

The remaining scanner hits are domain/policy helpers rather than pure reusable math:

- `RendererSettingsViewModel.NormalizeMaxRenderWorkers` and `SettingsWindow.NormalizeMaxRenderWorkers`: UI policy over `WorkerBudget.LogicalProcessorCount`, not a standalone numeric helper.
- `BuildMeshTopologyStep.ResolveEncounterStart` and `ResolveEncounterEnd`: topology-domain choice based on directed edge encounter order.
- `DefaultClassifyEdgesToFragmentsStep.IsFaceVisibleFast` and `IsFrontFacing`: graph/domain predicates over `ProjectedTriangle` and `faceVisible`, not primitive geometry.
- `BuildProjectedTrianglesStep.TryBuildTriangle`: an NPR pipeline assembly method that uses Common Math internally but owns graph/domain object construction.

These should stay local unless their domain types are moved or a broader boundary refactor is requested.

## Completion Criteria

- Every `target_math_api` class referenced in the CSV now exists under `src/aot/STFU.Common/Math`.
- Pure primitive math implementations were moved or wrapped in `STFU.Common.Math` APIs.
- Call sites use Common Math for numeric, raster, visibility, geometry, projection, path, stroke, color, hash/signature, diff/metric, GPU packing, buffer sizing, animation sampling, and transform helpers.
- Remaining local methods are domain object orchestration/policy rather than reusable math.
