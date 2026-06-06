using System.Numerics;
using STFU.Abstractions.Modules;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Engine;
using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Engine.Scenes;
using STFU.Mesh;
using STFU.Mesh.Commands;
using STFU.Mesh.Loading;
using STFU.MeshIO;
using STFU.Messaging.Commands;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.NPR.Pipelines;
using STFU.NPR.Presets;
using STFU.NPR.Temporal;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Cpu;
using STFU.Strokes;
using Xunit;

namespace STFU.NPR.Parity.Tests;

public sealed class NprPipelineWorkerParityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(24)]
    public void DefaultPipeline_WorkerCounts_ProduceSameStructuralHash(int workerCount)
    {
        var baseline = RenderDefaultPipelineSnapshot(1);
        var actual = RenderDefaultPipelineSnapshot(workerCount);

        Assert.Equal(baseline, actual);
    }

    private static NprRenderParitySnapshot RenderDefaultPipelineSnapshot(int workerCount)
    {
        var engine = CreateEngine();
        var renderer = engine.Registry.GetRequired<INprRenderer>();
        var assets = engine.Registry.GetRequired<AssetRegistry>();
        var camera = engine.Registry.GetRequired<CameraRig>();
        var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
        var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
        var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
        var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

        presetState.ApplyPreset("default");
        frameHistory.Reset();
        var budget = new NprFrameBudget(
            MaxWorkerThreads: workerCount,
            WorkerBudgetMode: WorkerBudgetMode.Performance);

        var request = new NprRenderRequest(
            Revision: 1,
            Width: 800,
            Height: 600,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
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
            Quality: NprQualityProfile.Default,
            Budget: budget,
            Theme: NprRenderTheme.Light,
            ShowGrid: false,
            IncludeDebugFrame: true,
            DebugOverlay: DebugOverlayKind.None,
            DiagnosticsOptions: new NprDiagnosticsOptions(
                EnablePassTimings: true,
                EnableStepAllocationTracking: true,
                EnableDetailedStepNotes: true,
                EnableMemoryLogs: false,
                EnablePixelHash: true,
                EnableFrameHash: true),
            OptimizerMode: NprRenderOptimizerMode.On);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        Assert.Equal(budget.ResolveWorkerCount(), result.Diagnostics.WorkerCount);
        var surface = result.PixelSurfaceLease?.Surface
            ?? throw new InvalidOperationException("Parity render did not produce PixelSurface output.");

        return new NprRenderParitySnapshot(
            Revision: result.Revision,
            Width: surface.Width,
            Height: surface.Height,
            ExecutionProfile: result.ExecutionProfile,
            ContentKind: request.ContentKind,
            ActivePresetId: request.ActivePresetId,
            ActivePipelineId: request.ActivePipelineId,
            StrokeFrameHash: NprRenderParityHasher.HashStrokeFrame(result.StrokeFrame),
            NprFrameHash: NprRenderParityHasher.HashNprFrame(result.NprFrame),
            DebugFrameHash: NprRenderParityHasher.HashDebugFrame(result.DebugFrame),
            PixelHash: NprRenderParityHasher.HashPixelSurface(surface),
            PathCount: result.StrokeFrame.Paths.Count,
            LayerCount: result.NprFrame.Layers.Count,
            ToneSurfaceCount: result.Diagnostics.ToneSurfaceCount);
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
        foreach (var root in EnumerateRoots())
        {
            var path = Path.GetFullPath(Path.Combine(root, "assets", fileName));
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"Asset '{fileName}' was not found in any known root.");
    }

    private static IEnumerable<string> EnumerateRoots()
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
        var maxDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        var scale = maxDimension > 1e-6f ? 1.45f / maxDimension : 1f;
        var scaleVector = new Vector3(scale, scale, scale);

        return new Transform3D(
            Position: -center * scale,
            Rotation: Vector3.Zero,
            Scale: scaleVector);
    }
}
