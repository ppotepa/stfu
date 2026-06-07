using System.Numerics;
using STFU.Abstractions.Modules;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Primitives;
using STFU.Engine;
using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Mesh;
using STFU.Mesh.Commands;
using STFU.Mesh.Loading;
using STFU.MeshIO;
using STFU.Messaging.Commands;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Pipelines;
using STFU.NPR.Presets;
using STFU.NPR.Debug;
using STFU.NPR.Temporal;
using STFU.Common.Math;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX;
using STFU.Rendering.Cpu;
using STFU.Strokes;
using Xunit;

namespace STFU.NPR.Parity.Tests;

public sealed class NprDirectXRuntimeDiagnosticsTests
{
    [Fact]
    public void DirectXBackend_RuntimeDiagnostics_ReportReadbacksAndVisibility()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var engine = CreateEngine();
        if (!engine.Registry.TryGet<IGpuRenderBackend>(out var gpu) || !gpu.IsAvailable)
        {
            return;
        }

        using var presentResult = RenderFrame(
            engine,
            "present",
            new NprFrameBudget(RequireGpuReadback: false, AllowGpuReadback: false, PreferGpuPresentation: true),
            NprQualityProfile.Default);
        Assert.Equal(NprExecutionProfile.CpuDrivenGpuAccelerated, presentResult.ExecutionProfile);
        Assert.Equal(NprRenderOutputKind.GpuTexture, presentResult.OutputKind);
        Assert.Equal(0, presentResult.Diagnostics.Readbacks);

        using var readbackResult = RenderFrame(
            engine,
            "readback",
            new NprFrameBudget(RequireGpuReadback: true, AllowGpuReadback: true, PreferGpuPresentation: false),
            NprQualityProfile.Default);
        Assert.Equal(NprExecutionProfile.CpuDrivenGpuAccelerated, readbackResult.ExecutionProfile);
        Assert.Equal(NprRenderOutputKind.PixelSurface, readbackResult.OutputKind);
        Assert.True(readbackResult.Diagnostics.Readbacks > 0);

        using var visibilityResult = RenderFrame(
            engine,
            "visibility",
            new NprFrameBudget(
                RequireGpuReadback: false,
                AllowGpuReadback: false,
                PreferGpuPresentation: true,
                GpuVisibilityRequiredMatchRatio: 1f),
            NprQualityProfile.Default with { UseGpuVisibilityBuffer = true });

        Assert.NotNull(visibilityResult.Diagnostics.VisibilityParity);
        var visibilityStats = visibilityResult.Diagnostics.VisibilityParity!;
        Assert.InRange(visibilityStats.MatchingFaces, 0, visibilityStats.CpuVisibleFaces);
        Assert.InRange(visibilityStats.MismatchCount, 0, int.MaxValue);

        if (visibilityStats.ShouldFallback(1f))
        {
            Assert.Equal(NprExecutionProfile.FullCpuReference, visibilityResult.ExecutionProfile);
            Assert.True(visibilityStats.FallbackUsed);
            Assert.False(string.IsNullOrWhiteSpace(visibilityStats.FallbackReason));
        }
        else
        {
            Assert.Equal(NprExecutionProfile.CpuDrivenGpuAccelerated, visibilityResult.ExecutionProfile);
            Assert.False(visibilityStats.FallbackUsed);
        }
    }

    private static NprRenderResult RenderFrame(
        StfuEngine? engine,
        string label,
        NprFrameBudget budget,
        NprQualityProfile quality)
    {
        var renderer = engine.Registry.GetRequired<INprRenderer>();
        var assets = engine.Registry.GetRequired<AssetRegistry>();
        var camera = engine.Registry.GetRequired<CameraRig>();
        var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
        var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
        var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
        var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

        if (label == "visibility" && !quality.UseGpuVisibilityBuffer)
        {
            Assert.Fail("visibility label requires visibility quality option.");
        }

        if (!string.IsNullOrWhiteSpace(presetState.ActivePreset.Metadata.Id))
        {
            presetState.ApplyPreset("default");
            frameHistory.Reset();
        }

        var request = new NprRenderRequest(
            Revision: 1,
            Width: 256,
            Height: 256,
            ExecutionProfile: NprExecutionProfile.CpuDrivenGpuAccelerated,
            ContentKind: NprRenderContentKind.NprPipeline,
            Scene: engine.Scene,
            Assets: assets,
            Camera: camera.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: entityStyles,
            Analysis: analysis,
            FrameHistoryState: frameHistory,
            Pipeline: presetState.ActivePipeline,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: frameHistory.PeekNextFrameId(),
            TimeSeconds: 1f / 60f,
            PreviousFrame: frameHistory.GetPreviousFrame(),
            Quality: quality,
            Budget: budget,
            Theme: NprRenderTheme.Light,
            ShowGrid: false,
            IncludeDebugFrame: false,
            DebugOverlay: DebugOverlayKind.None,
            DiagnosticsOptions: null,
            OptimizerMode: NprRenderOptimizerMode.On);

        return renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    private static StfuEngine CreateEngine()
    {
        var engine = StfuEngineBuilder
            .Create()
            .AddModule(new AssetsModule())
            .AddModule(new MeshModule())
            .AddModule(new MeshIOModule())
            .AddModule(new CameraModule())
            .AddModule(new StrokesModule())
            .AddModule(new NprModule(
                BuiltInNprPresets.CreateAll(),
                BuiltInNprPipelines.CreateAll()))
            .AddModule(new CpuRenderingModule())
            .AddModule(new DirectXRenderingModule())
            .Build();

        var commands = new CommandBuffer();
        commands.Enqueue(new CreateEntityCommand("Suzanne"));
        engine.Tick(commands);

        var camera = engine.Registry.GetRequired<CameraRig>();
        camera.SetCamera(new CameraState(
            new Vector3(0f, 0f, 4f),
            Vector3.Zero,
            45f));

        var entity = engine.Scene.Entities[0];
        var meshHandle = LoadSuzanneMesh(engine);
        commands.Enqueue(new AssignMeshToEntityCommand(entity.Id, meshHandle));
        engine.Tick(commands);

        var assets = engine.Registry.GetRequired<AssetRegistry>();
        if (assets.TryGetMesh(meshHandle, out var mesh))
        {
            entity.Transform = CreateParityTransform(mesh);
        }

        return engine;
    }

    private static MeshHandle LoadSuzanneMesh(StfuEngine engine)
    {
        var meshFactory = engine.Registry.GetRequired<MeshFactory>();
        var meshLoader = engine.Registry.GetRequired<IMeshLoader<string>>();
        var assets = engine.Registry.GetRequired<AssetRegistry>();
        var path = ResolveAssetPath("suzanne.obj");
        var mesh = meshFactory.Load(path, meshLoader);
        return assets.AddMesh(path, mesh);
    }

    private static string ResolveAssetPath(string fileName)
    {
        foreach (var root in EnumerateAssetRoots())
        {
            var path = Path.GetFullPath(Path.Combine(root, "assets", fileName));
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"Asset '{fileName}' was not found in any known root.");
    }

    private static IEnumerable<string> EnumerateAssetRoots()
    {
        yield return Environment.CurrentDirectory;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private static Transform3D CreateParityTransform(MeshData mesh)
    {
        if (mesh.Vertices.Count == 0)
        {
            return Transform3D.Identity;
        }

        var min = mesh.Vertices[0].Position;
        var max = mesh.Vertices[0].Position;
        for (var index = 1; index < mesh.Vertices.Count; index++)
        {
            var position = mesh.Vertices[index].Position;
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        var center = (min + max) * 0.5f;
        var size = max - min;
        var maxDimension = Geometry3D.MaxComponent(size);
        var scale = maxDimension > 1e-6f ? 1.45f / maxDimension : 1f;
        var scaleVector = new Vector3(scale, scale, scale);

        return new Transform3D(
            Position: -center * scale,
            Rotation: Vector3.Zero,
            Scale: scaleVector);
    }
}
