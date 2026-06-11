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
using STFU.NPR.Pipelines;
using STFU.NPR.Presets;
using STFU.NPR.Temporal;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Cpu;
using STFU.Strokes;
using Xunit;

namespace STFU.NPR.Parity.Tests;

public sealed class NprPipelineWorkerSweepDiagnosticsTests
{
    [Fact]
    public void Range_timings_are_empty_when_disabled()
    {
        var result = RenderWithDiagnostics(workerCount: 4, enableRangeTimings: false);

        Assert.DoesNotContain(
            result.StepNotes,
            note => note.Contains("rangeTicks", StringComparison.Ordinal));
    }

    [Fact]
    public void Range_timings_are_recorded_when_enabled()
    {
        var result = RenderWithDiagnostics(workerCount: 4, enableRangeTimings: true);

        Assert.Contains(
            result.StepNotes,
            note => note.Contains("rangeTicks", StringComparison.Ordinal));
        Assert.Contains(
            result.StepNotes,
            note => note.Contains("ranges=", StringComparison.Ordinal));
    }

    private static RenderDiagnosticsResult RenderWithDiagnostics(int workerCount, bool enableRangeTimings)
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
            DiagnosticsOptions: NprDiagnosticsOptions.Parity with
            {
                EnableDetailedStepNotes = true,
                EnableRangeTimings = enableRangeTimings
            },
            OptimizerMode: NprRenderOptimizerMode.On);

        using var renderResult = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        var stepNotes = renderResult.Diagnostics.Timings
            .Select(timing => timing.Notes ?? string.Empty)
            .Where(note => !string.IsNullOrWhiteSpace(note))
            .ToArray();

        return new RenderDiagnosticsResult(stepNotes);
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

    private sealed record RenderDiagnosticsResult(IReadOnlyList<string> StepNotes);
}
