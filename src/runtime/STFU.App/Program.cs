using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Math;
using STFU.Engine;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.NPR.Pipeline;
using STFU.NPR.Temporal;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.DirectX.Device;
using STFU.Import.Fbx;
using STFU.Logging;
using STFU.Mesh;
using STFU.UI;
using STFU.UI.Bridge.Session;
using STFU.Rendering.Abstractions.Diagnostics;

var logSession = StfuLogSession.Start();
StfuLog.Configure(logSession, Console.WriteLine);
logSession.WriteMetadata(args);
RegisterGlobalExceptionLogging();
WriteLog("Starting STFU host.");
WriteLog($"Log run directory: {logSession.RunDirectory}");

try
{
    if (args.Length > 0 && string.Equals(args[0], "--compare-default-snapshots", StringComparison.OrdinalIgnoreCase))
    {
        CompareDefaultSnapshots(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--dump-default-snapshot", StringComparison.OrdinalIgnoreCase))
    {
        DumpDefaultSnapshot(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--inspect-default-edge-visibility", StringComparison.OrdinalIgnoreCase))
    {
        InspectDefaultEdgeVisibility(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--probe-fbx", StringComparison.OrdinalIgnoreCase))
    {
        var path = args.Length > 1 ? args[1] : Path.Combine("assets", "walking.fbx");
        var context = STFU.Abstractions.Loading.LoadContext.Default;
        if (args.Length > 2 && int.TryParse(args[2], out var animationIndex))
        {
            context.Set(STFU.Import.AssetImportContextKeys.AnimationIndex, animationIndex);
        }

        if (args.Length > 3 && double.TryParse(args[3], out var timeSeconds))
        {
            context.Set(STFU.Import.AssetImportContextKeys.TimeSeconds, timeSeconds);
        }

        var loader = new FbxAssetLoader();
        var result = loader.Load(path, context);

        if (!result.Success)
        {
            WriteLog($"FBX probe failed: {result.Error}");
            Environment.ExitCode = 1;
            return;
        }

        var asset = result.Value!;
        WriteLog($"FBX probe loaded '{asset.SourcePath}'.");
        WriteLog($"Meshes={asset.Meshes.Count}, skinnedMeshes={asset.SkinnedMeshes.Count}, skeletons={asset.Skeletons.Count}, animations={asset.Animations.Count}.");

        var vertexCount = asset.Meshes.Sum(mesh => mesh.Mesh.Vertices.Count);
        var triangleCount = asset.Meshes.Sum(mesh => mesh.Mesh.Triangles.Count);
        WriteLog($"Baked mesh data: vertices={vertexCount}, triangles={triangleCount}.");

        foreach (var item in asset.Metadata)
        {
            WriteLog($"{item.Key}={item.Value}");
        }

        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--smoke-fullcpu", StringComparison.OrdinalIgnoreCase))
    {
        SmokeFullCpu(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--smoke-gpu-readback", StringComparison.OrdinalIgnoreCase))
    {
        SmokeGpuReadback(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--smoke-gpu-present", StringComparison.OrdinalIgnoreCase))
    {
        SmokeGpuPresent(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--verify-render-parity", StringComparison.OrdinalIgnoreCase))
    {
        VerifyRenderParity(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--verify-gpu-mesh-parity", StringComparison.OrdinalIgnoreCase))
    {
        VerifyGpuMeshParity(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--smoke-fbx-playback", StringComparison.OrdinalIgnoreCase))
    {
        SmokeFbxPlayback(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--smoke-fbx-ui-load", StringComparison.OrdinalIgnoreCase))
    {
        SmokeFbxUiLoad(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--smoke-fbx-abi", StringComparison.OrdinalIgnoreCase))
    {
        SmokeFbxAbi(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--bench-fbx-mesh-render", StringComparison.OrdinalIgnoreCase))
    {
        BenchFbxMeshRender(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--bench-gpu-render", StringComparison.OrdinalIgnoreCase))
    {
        BenchGpuRender(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--bench-render-readback", StringComparison.OrdinalIgnoreCase))
    {
        BenchRenderReadback(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--bench-render-grid", StringComparison.OrdinalIgnoreCase))
    {
        BenchRenderGrid(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--bench-render-alloc", StringComparison.OrdinalIgnoreCase))
    {
        BenchRenderAlloc(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--bench-render-resize", StringComparison.OrdinalIgnoreCase))
    {
        BenchRenderResize(args);
        return;
    }

    if (args.Length > 0 && string.Equals(args[0], "--bench-render-profiles", StringComparison.OrdinalIgnoreCase))
    {
        BenchRenderProfiles(args);
        return;
    }

    StfuUiHost.Run(args, message => StfuLog.Write(StfuLogDomain.Ui, message));
    WriteLog("STFU UI stopped.");
}
catch (Exception exception)
{
    StfuLog.Write(StfuLogDomain.Errors, "fatal", "Fatal error.", StfuLogLevel.Fatal, exception: exception);
    Environment.ExitCode = 1;
}
finally
{
    StfuLog.Shutdown();
}

static void WriteLog(string message)
{
    StfuLog.Write(StfuLogDomain.Host, message);
}

static void RegisterGlobalExceptionLogging()
{
    AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
    {
        var exception = eventArgs.ExceptionObject as Exception;
        StfuLog.Write(
            StfuLogDomain.Errors,
            "unhandled_exception",
            exception?.Message ?? "Unhandled non-Exception object.",
            StfuLogLevel.Fatal,
            exception: exception);
    };

    TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
    {
        StfuLog.Write(
            StfuLogDomain.Errors,
            "unobserved_task_exception",
            eventArgs.Exception.Message,
            StfuLogLevel.Error,
            exception: eventArgs.Exception);
    };
}

static void DumpDefaultSnapshot(string[] args)
{
    var outputPath = args.Length > 1
        ? args[1]
        : Path.Combine("artifacts", "default-parity-snapshot.json");
    var presetId = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
        ? args[2]
        : "default";
    var width = TryParsePositiveInt(args, 3, 800);
    var height = TryParsePositiveInt(args, 4, 600);

    WriteLog($"Creating default parity snapshot: preset={presetId}, size={width}x{height}.");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    presetState.ApplyPreset(presetId);

    if (!string.Equals(presetState.ActivePreset.PipelineId, NprPipelineIds.Default, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Preset '{presetId}' does not use pipeline '{NprPipelineIds.Default}'.");
    }

    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();
    var frameId = frameHistory.PeekNextFrameId();

    var context = new NprContext
    {
        FrameId = frameId,
        TimeSeconds = frameId / 60f,
        PreviousFrame = frameHistory.GetPreviousFrame(),
        Scene = engine.Scene,
        Assets = assets,
        Camera = camera.Camera,
        Width = width,
        Height = height,
        Settings = presetState.ActiveSettings,
        Style = presetState.ActiveGrammar,
        StyleSet = presetState.ActiveStyleSet,
        EntityStyles = entityStyles,
        Analysis = analysis,
        FrameHistoryState = frameHistory
    };

    presetState.ActivePipeline.Execute(context);

    var fullPath = Path.GetFullPath(outputPath);
    var directory = Path.GetDirectoryName(fullPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.WriteAllText(fullPath, DefaultParitySnapshotBuilder.ToJson(context, indented: true));

    WriteLog($"Default parity snapshot written: {fullPath}");
    WriteLog(
        $"Snapshot stats: vertices={context.Graph.Vertices.Count}, triangles={context.Graph.Triangles.Count}, " +
        $"fragments={context.Graph.DefaultFragments.Count}, paths={context.Graph.DefaultPaths.Count}, " +
        $"drawablePaths={context.Graph.DefaultDrawablePaths.Count}, strokes={context.Frame.Paths.Count}.");
}

static void InspectDefaultEdgeVisibility(string[] args)
{
    if (args.Length < 5)
    {
        throw new InvalidOperationException(
            "Usage: --inspect-default-edge-visibility <preset> <width> <height> <edgeStableId> [edgeStableId...]");
    }

    var presetId = args[1];
    var width = TryParsePositiveInt(args, 2, 800);
    var height = TryParsePositiveInt(args, 3, 600);
    var edgeIds = args
        .Skip(4)
        .Select(value => int.TryParse(value, out var edgeId)
            ? edgeId
            : throw new InvalidOperationException($"Invalid edgeStableId: {value}"))
        .ToArray();

    var context = CreateDefaultPipelineContext(presetId, width, height);
    var buffer = context.Graph.DefaultFaceIdVisibility
        ?? throw new InvalidOperationException("DefaultFaceIdVisibility buffer was not created.");

    foreach (var edgeId in edgeIds)
    {
        var matches = context.Graph.TopologyEdges
            .Where(edge => edge.StableId == edgeId)
            .ToArray();

        if (matches.Length == 0)
        {
            WriteLog($"Edge {edgeId}: not found.");
            continue;
        }

        for (var matchIndex = 0; matchIndex < matches.Length; matchIndex++)
        {
            var edge = matches[matchIndex];
            var start = context.Graph.Vertices[edge.StartVertexIndex];
            var end = context.Graph.Vertices[edge.EndVertexIndex];
            var dx = end.Position.X - start.Position.X;
            var dy = end.Position.Y - start.Position.Y;
            var length = Geometry2D.SegmentLength(start.Position.X, start.Position.Y, end.Position.X, end.Position.Y);
            var samples = NumericMath.AtMost(96, NumericMath.AtLeast((int)NumericMath.Ceiling(length / 4f), 7));

            WriteLog(
                $"Edge {edgeId}#{matchIndex}: tri={edge.FirstTriangleIndex}/{edge.SecondTriangleIndex}, " +
                $"verts={edge.StartVertexIndex}->{edge.EndVertexIndex}, boundary={edge.IsBoundary}, len={length:0.###}, samples={samples}");

            for (var sampleIndex = 0; sampleIndex <= samples; sampleIndex++)
            {
                var t = sampleIndex / (float)samples;
                var point = new STFU.Strokes.Point2D(
                    start.Position.X + (end.Position.X - start.Position.X) * t,
                    start.Position.Y + (end.Position.Y - start.Position.Y) * t);
                var ndc = Vector3.Lerp(start.Ndc, end.Ndc, t);
                var inClip = !(ndc.X < -1f || ndc.X > 1f || ndc.Y < -1f || ndc.Y > 1f || ndc.Z < -1f || ndc.Z > 1f);
                var cx = buffer.ToBufferX(point.X, width);
                var cy = buffer.ToBufferY(point.Y, height);
                var centerFace = buffer.FaceId[cy * buffer.Width + cx];
                var centerOwned =
                    (edge.FirstTriangleIndex >= 0 && centerFace == edge.FirstTriangleIndex) ||
                    (edge.SecondTriangleIndex >= 0 && centerFace == edge.SecondTriangleIndex);
                var neighborhoodOwned = buffer.SampleOwnedFaceAtScreen(
                    point.X,
                    point.Y,
                    width,
                    height,
                    edge.FirstTriangleIndex,
                    edge.SecondTriangleIndex);

                WriteLog(
                    $"  sample {sampleIndex}/{samples}: t={t:0.###}, p=({point.X:0.###},{point.Y:0.###}), buf=({cx},{cy}), centerFace={centerFace}, centerOwned={centerOwned}, owned3x3={neighborhoodOwned}, inClip={inClip}");
            }
        }
    }
}

static void CompareDefaultSnapshots(string[] args)
{
    if (args.Length < 3)
    {
        throw new InvalidOperationException(
            "Usage: --compare-default-snapshots <left.json> <right.json>");
    }

    var leftPath = Path.GetFullPath(args[1]);
    var rightPath = Path.GetFullPath(args[2]);
    var left = DefaultParitySnapshotBuilder.FromJson(File.ReadAllText(leftPath))
        ?? throw new InvalidOperationException($"Could not deserialize snapshot: {leftPath}");

    var right = DefaultParitySnapshotBuilder.FromJson(File.ReadAllText(rightPath))
        ?? throw new InvalidOperationException($"Could not deserialize snapshot: {rightPath}");

    var comparison = DefaultParitySnapshotComparer.Compare(left, right);
    WriteLog(comparison.ToConsoleReport());
}

static NprContext CreateDefaultPipelineContext(string presetId, int width, int height)
{
    var engine = StfuRuntimeBootstrap.CreateEngine();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    presetState.ApplyPreset(presetId);

    if (!string.Equals(presetState.ActivePreset.PipelineId, NprPipelineIds.Default, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Preset '{presetId}' does not use pipeline '{NprPipelineIds.Default}'.");
    }

    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();
    var frameId = frameHistory.PeekNextFrameId();

    var context = new NprContext
    {
        FrameId = frameId,
        TimeSeconds = frameId / 60f,
        PreviousFrame = frameHistory.GetPreviousFrame(),
        Scene = engine.Scene,
        Assets = assets,
        Camera = camera.Camera,
        Width = width,
        Height = height,
        Settings = presetState.ActiveSettings,
        Style = presetState.ActiveGrammar,
        StyleSet = presetState.ActiveStyleSet,
        EntityStyles = entityStyles,
        Analysis = analysis,
        FrameHistoryState = frameHistory
    };

    presetState.ActivePipeline.Execute(context);
    return context;
}

static int TryParsePositiveInt(string[] args, int index, int fallback)
{
    return args.Length > index && int.TryParse(args[index], out var value) && value > 0
        ? value
        : fallback;
}

static bool HasFlag(string[] args, string flag)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}

static void SmokeFullCpu(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var enableRangeTimings = HasFlag(args, "--npr-range-timings");

    WriteLog($"Running Full CPU smoke test at {width}x{height}. rangeTimings={enableRangeTimings}");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    RunCase("mesh", applyPreset: null, NprRenderContentKind.MeshWireframe, expectedPipeline: null);
    RunCase("default", applyPreset: "default", NprRenderContentKind.NprPipeline, expectedPipeline: NprPipelineIds.Default);
    RunCase("comic-surface", applyPreset: "comic-surface", NprRenderContentKind.NprPipeline, expectedPipeline: NprPipelineIds.ComicSurface);

    WriteLog("Full CPU smoke test passed.");

    void RunCase(
        string label,
        string? applyPreset,
        NprRenderContentKind contentKind,
        string? expectedPipeline)
    {
        if (!string.IsNullOrWhiteSpace(applyPreset))
        {
            presetState.ApplyPreset(applyPreset);
            frameHistory.Reset();
        }

        var request = new NprRenderRequest(
            Revision: 1,
            Width: width,
            Height: height,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
            ContentKind: contentKind,
            Scene: engine.Scene,
            Assets: assets,
            Camera: camera.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: entityStyles,
            Analysis: analysis,
            FrameHistoryState: frameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: frameHistory.PeekNextFrameId(),
            TimeSeconds: 1f / 60f,
            PreviousFrame: frameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(),
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            DiagnosticsOptions: CreateSmokeDiagnosticsOptions(enableRangeTimings),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"Smoke case '{label}' did not complete. Status={result.Status}.");
        }

        if (result.OutputKind != NprRenderOutputKind.PixelSurface || result.PixelSurfaceLease is null)
        {
            throw new InvalidOperationException($"Smoke case '{label}' did not produce a pixel surface.");
        }

        var surface = result.PixelSurfaceLease.Surface;
        if (surface.Width != width || surface.Height != height || surface.ByteLength <= 0)
        {
            throw new InvalidOperationException($"Smoke case '{label}' produced an invalid surface.");
        }

        if (expectedPipeline is not null &&
            !string.Equals(request.ActivePipelineId, expectedPipeline, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Smoke case '{label}' expected pipeline '{expectedPipeline}' but used '{request.ActivePipelineId}'.");
        }

        WriteLog(
            $"Full CPU smoke '{label}' ok: status={result.Status}, output={result.OutputKind}, " +
            $"paths={result.StrokeFrame.Paths.Count}, layers={result.NprFrame.Layers.Count}, tones={result.Diagnostics.ToneSurfaceCount}, " +
            $"total={result.Diagnostics.TotalMilliseconds:0.00}ms.");
    }
}

static void SmokeGpuReadback(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var useGpuVisibility = HasFlag(args, "--gpu-visibility");
    var enableRangeTimings = HasFlag(args, "--npr-range-timings");

    WriteLog($"Running GPU readback smoke test at {width}x{height}. gpuVisibility={useGpuVisibility} rangeTimings={enableRangeTimings}");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    if (!engine.Registry.TryGet<IGpuRenderBackend>(out var gpu) || !gpu.IsAvailable)
    {
        throw new InvalidOperationException("GPU backend is not available.");
    }

    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    RunCase("mesh", null, NprRenderContentKind.MeshWireframe, expectedPipeline: null);
    RunCase("default", "default", NprRenderContentKind.NprPipeline, expectedPipeline: NprPipelineIds.Default, requireVisibleInk: true);
    RunCase("default-feature-curves", "default", NprRenderContentKind.NprPipeline, expectedPipeline: NprPipelineIds.Default, debugOverlay: DebugOverlayKind.FeatureCurves, requireVisibleInk: true);
    RunCase("comic-surface", "comic-surface", NprRenderContentKind.NprPipeline, expectedPipeline: NprPipelineIds.ComicSurface);

    WriteLog("GPU readback smoke test passed.");

    void RunCase(
        string label,
        string? applyPreset,
        NprRenderContentKind contentKind,
        string? expectedPipeline,
        DebugOverlayKind debugOverlay = DebugOverlayKind.None,
        bool requireVisibleInk = false)
    {
        if (!string.IsNullOrWhiteSpace(applyPreset))
        {
            presetState.ApplyPreset(applyPreset);
            frameHistory.Reset();
        }

        var request = new NprRenderRequest(
            Revision: 1,
            Width: width,
            Height: height,
            ExecutionProfile: NprExecutionProfile.CpuDrivenGpuAccelerated,
            ContentKind: contentKind,
            Scene: engine.Scene,
            Assets: assets,
            Camera: camera.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: entityStyles,
            Analysis: analysis,
            FrameHistoryState: frameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: frameHistory.PeekNextFrameId(),
            TimeSeconds: 1f / 60f,
            PreviousFrame: frameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default with { UseGpuVisibilityBuffer = useGpuVisibility },
            Budget: new NprFrameBudget(RequireGpuReadback: true, PreferGpuPresentation: false),
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: debugOverlay != DebugOverlayKind.None,
            DebugOverlay: debugOverlay,
            DiagnosticsOptions: CreateSmokeDiagnosticsOptions(enableRangeTimings),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"GPU smoke case '{label}' did not complete. Status={result.Status}.");
        }

        if (result.OutputKind != NprRenderOutputKind.PixelSurface || result.PixelSurfaceLease is null)
        {
            throw new InvalidOperationException($"GPU smoke case '{label}' did not produce readback PixelSurface.");
        }

        if (expectedPipeline is not null &&
            !string.Equals(request.ActivePipelineId, expectedPipeline, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"GPU smoke case '{label}' expected pipeline '{expectedPipeline}' but used '{request.ActivePipelineId}'.");
        }

        var surface = result.PixelSurfaceLease.Surface;
        if (surface.Width != width || surface.Height != height || surface.ByteLength <= 0)
        {
            throw new InvalidOperationException($"GPU smoke case '{label}' produced an invalid surface.");
        }

        var nonPaperPixels = CountNonPaperPixels(surface, request.Theme);
        if (requireVisibleInk && nonPaperPixels <= 0)
        {
            throw new InvalidOperationException($"GPU smoke case '{label}' produced a paper-only image.");
        }

        WriteLog(
            $"GPU smoke '{label}' ok: output={result.OutputKind}, paths={result.Diagnostics.PathCount}, visiblePixels={nonPaperPixels}, " +
            $"layers={result.NprFrame.Layers.Count}, debugLines={result.DebugFrame.Lines.Count}, " +
            $"total={result.Diagnostics.TotalMilliseconds:0.00}ms.");

        if (useGpuVisibility)
        {
            foreach (var timing in result.Diagnostics.Timings.Where(t => t.Name == "GpuVisibilityBuffer"))
            {
                WriteLog($"GPU smoke '{label}' visibility: {timing.Milliseconds:0.00}ms; {timing.Notes}");
            }
        }
    }
}

static int CountNonPaperPixels(PixelSurface surface, NprRenderTheme theme)
{
    const int tolerance = 3;
    var paper = theme.PaperColor;
    var count = 0;
    var pixels = surface.Pixels;

    for (var y = 0; y < surface.Height; y++)
    {
        var row = y * surface.Stride;
        for (var x = 0; x < surface.Width; x++)
        {
            var offset = row + x * 4;
            var b = pixels[offset];
            var g = pixels[offset + 1];
            var r = pixels[offset + 2];
            var a = pixels[offset + 3];

            if (NumericMath.Abs(r - paper.R) > tolerance ||
                NumericMath.Abs(g - paper.G) > tolerance ||
                NumericMath.Abs(b - paper.B) > tolerance ||
                NumericMath.Abs(a - 255) > tolerance)
            {
                count++;
            }
        }
    }

    return count;
}

static void SmokeGpuPresent(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var enableRangeTimings = HasFlag(args, "--npr-range-timings");

    WriteLog($"Running GPU direct-present smoke test at {width}x{height}. rangeTimings={enableRangeTimings}");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    if (!engine.Registry.TryGet<IGpuRenderBackend>(out var gpu) || !gpu.IsAvailable)
    {
        throw new InvalidOperationException("GPU backend is not available.");
    }

    var device = engine.Registry.GetRequired<DirectXDevice>();
    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    using var smokeWindow = new Win32SmokeWindow(width, height);
    using var swapChain = new DirectXSwapChain(device);
    using (device.Lock())
    {
        swapChain.AttachOrResize(smokeWindow.Handle, width, height);
    }

    RunCase("mesh", null, NprRenderContentKind.MeshWireframe, null);
    RunCase("default", "default", NprRenderContentKind.NprPipeline, NprPipelineIds.Default);
    RunCase("default-feature-curves", "default", NprRenderContentKind.NprPipeline, NprPipelineIds.Default, DebugOverlayKind.FeatureCurves);

    WriteLog("GPU direct-present smoke test passed.");

    void RunCase(
        string label,
        string? applyPreset,
        NprRenderContentKind contentKind,
        string? expectedPipeline,
        DebugOverlayKind debugOverlay = DebugOverlayKind.None)
    {
        if (!string.IsNullOrWhiteSpace(applyPreset))
        {
            presetState.ApplyPreset(applyPreset);
            frameHistory.Reset();
        }

        var request = new NprRenderRequest(
            Revision: 1,
            Width: width,
            Height: height,
            ExecutionProfile: NprExecutionProfile.CpuDrivenGpuAccelerated,
            ContentKind: contentKind,
            Scene: engine.Scene,
            Assets: assets,
            Camera: camera.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: entityStyles,
            Analysis: analysis,
            FrameHistoryState: frameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: frameHistory.PeekNextFrameId(),
            TimeSeconds: 1f / 60f,
            PreviousFrame: frameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(
                RequireGpuReadback: false,
                AllowGpuReadback: false,
                PreferGpuPresentation: true),
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: debugOverlay != DebugOverlayKind.None,
            DebugOverlay: debugOverlay,
            DiagnosticsOptions: CreateSmokeDiagnosticsOptions(enableRangeTimings),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"GPU direct-present smoke '{label}' did not complete. Status={result.Status}.");
        }

        if (result.OutputKind != NprRenderOutputKind.GpuTexture || result.GpuTextureLease is null)
        {
            throw new InvalidOperationException($"GPU direct-present smoke '{label}' did not produce a GPU texture.");
        }

        if (expectedPipeline is not null &&
            !string.Equals(request.ActivePipelineId, expectedPipeline, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"GPU direct-present smoke '{label}' expected pipeline '{expectedPipeline}' but used '{request.ActivePipelineId}'.");
        }

        using (device.Lock())
        {
            if (!device.Resources.TryGetTexture(result.GpuTextureLease.Texture, out var texture))
            {
                throw new InvalidOperationException($"GPU direct-present smoke '{label}' could not resolve the render target.");
            }

            swapChain.PresentTexture(texture);
        }

        WriteLog(
            $"GPU direct-present '{label}' ok: output={result.OutputKind}, paths={result.StrokeFrame.Paths.Count}, " +
            $"layers={result.NprFrame.Layers.Count}, debugLines={result.DebugFrame.Lines.Count}, " +
            $"total={result.Diagnostics.TotalMilliseconds:0.00}ms.");
    }
}

static void BenchGpuRender(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);
    var frames = TryParsePositiveInt(args, 3, 30);
    var optimizerMode = ResolveRenderOptimizerMode(args);

    WriteLog($"Running GPU benchmark at {width}x{height} for {frames} frames.");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    if (!engine.Registry.TryGet<IGpuRenderBackend>(out var gpu) || !gpu.IsAvailable)
    {
        throw new InvalidOperationException("GPU backend is not available.");
    }

    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    RunCase("mesh", null, NprRenderContentKind.MeshWireframe, null);
    RunCase("default", "default", NprRenderContentKind.NprPipeline, NprPipelineIds.Default);
    RunCase("comic-surface", "comic-surface", NprRenderContentKind.NprPipeline, NprPipelineIds.ComicSurface);

    void RunCase(string label, string? presetId, NprRenderContentKind contentKind, string? expectedPipeline)
    {
        if (!string.IsNullOrWhiteSpace(presetId))
        {
            presetState.ApplyPreset(presetId);
        }

        frameHistory.Reset();
        var cpu = Measure(NprExecutionProfile.FullCpuReference, requireGpuReadback: false);
        frameHistory.Reset();
        var gpuResult = Measure(NprExecutionProfile.CpuDrivenGpuAccelerated, requireGpuReadback: true);

        WriteLog(
            $"Bench '{label}': CPU total={cpu.totalMs:0.00}ms ({NumericMath.FramesPerSecond(cpu.totalMs):0.0} FPS), " +
            $"GPU total={gpuResult.totalMs:0.00}ms ({NumericMath.FramesPerSecond(gpuResult.totalMs):0.0} FPS), " +
            $"GPU pipeline={gpuResult.pipelineMs:0.00}ms strokes={gpuResult.strokeMs:0.00}ms " +
            $"tones={gpuResult.toneMs:0.00}ms debug={gpuResult.debugMs:0.00}ms readback={gpuResult.readbackMs:0.00}ms.");

        (double totalMs, double pipelineMs, double strokeMs, double toneMs, double debugMs, double readbackMs) Measure(
            NprExecutionProfile profile,
            bool requireGpuReadback)
        {
            double totalMs = 0;
            double pipelineMs = 0;
            double strokeMs = 0;
            double toneMs = 0;
            double debugMs = 0;
            double readbackMs = 0;

            for (var frame = 0; frame < frames; frame++)
            {
                var request = new NprRenderRequest(
                    Revision: frame + 1,
                    Width: width,
                    Height: height,
                    ExecutionProfile: profile,
                    ContentKind: contentKind,
                    Scene: engine.Scene,
                    Assets: assets,
                    Camera: camera.Camera,
                    Settings: presetState.ActiveSettings,
                    Style: presetState.ActiveGrammar,
                    StyleSet: presetState.ActiveStyleSet,
                    EntityStyles: entityStyles,
                    Analysis: analysis,
                    FrameHistoryState: frameHistory,
                    Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
                    ActivePresetId: presetState.ActivePreset.Metadata.Id,
                    ActivePipelineId: presetState.ActivePreset.PipelineId,
                    FrameId: frameHistory.PeekNextFrameId(),
                    TimeSeconds: (frame + 1) / 60f,
                    PreviousFrame: frameHistory.GetPreviousFrame(),
                    Quality: NprQualityProfile.Default,
                    Budget: new NprFrameBudget(
                        RequireGpuReadback: requireGpuReadback,
                        PreferGpuPresentation: false),
                    Theme: NprRenderTheme.Light,
                    ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
                    IncludeDebugFrame: false,
                    DebugOverlay: DebugOverlayKind.None,
                    DiagnosticsOptions: CreateBenchmarkDiagnosticsOptions(),
                    OptimizerMode: optimizerMode);

                if (expectedPipeline is not null &&
                    !string.Equals(request.ActivePipelineId, expectedPipeline, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Benchmark '{label}' expected pipeline '{expectedPipeline}' but used '{request.ActivePipelineId}'.");
                }

                using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
                if (result.Status != NprRenderStatus.Completed)
                {
                    throw new InvalidOperationException($"GPU benchmark case '{label}' failed on frame {frame + 1}: {result.Status}");
                }

                totalMs += result.Diagnostics.TotalMilliseconds;
                pipelineMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "NprPipeline.Execute")?.Milliseconds ?? 0;
                strokeMs += result.Diagnostics.Timings.Where(t => t.Name == "GpuStrokeDraw").Sum(t => t.Milliseconds);
                toneMs += result.Diagnostics.Timings.Where(t => t.Name == "GpuToneSurfaceDraw").Sum(t => t.Milliseconds);
                debugMs += result.Diagnostics.Timings.Where(t => t.Name == "GpuDebugOverlayBuild" || t.Name == "GpuDebugOverlayDraw").Sum(t => t.Milliseconds);
                readbackMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuReadback")?.Milliseconds ?? 0;
            }

            var divisor = NumericMath.AtLeast(frames, 1);
            return (
                totalMs / divisor,
                pipelineMs / divisor,
                strokeMs / divisor,
                toneMs / divisor,
                debugMs / divisor,
                readbackMs / divisor);
        }
    }
}

static void BenchRenderReadback(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);
    var frames = TryParsePositiveInt(args, 3, 12);
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var presetId = args.Length > 4 && !string.IsNullOrWhiteSpace(args[4])
        ? args[4]
        : "default";

    WriteLog($"Benchmarking GPU readback: preset={presetId}, size={width}x{height}, frames={frames}.");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    if (!engine.Registry.TryGet<IGpuRenderBackend>(out var gpu) || !gpu.IsAvailable)
    {
        throw new InvalidOperationException("GPU backend is not available.");
    }

    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    presetState.ApplyPreset(presetId);
    frameHistory.Reset();

    if (!string.Equals(presetState.ActivePreset.PipelineId, NprPipelineIds.Default, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(presetState.ActivePreset.PipelineId, NprPipelineIds.ComicSurface, StringComparison.OrdinalIgnoreCase))
    {
        WriteLog($"Readback benchmark running on pipeline '{presetState.ActivePreset.PipelineId}'.");
    }

    double totalMs = 0;
    double renderMs = 0;
    double pipelineMs = 0;
    double drawMs = 0;
    double readbackMs = 0;
    long allocatedBytes = 0;

    for (var frame = 1; frame <= frames; frame++)
    {
        var request = new NprRenderRequest(
            Revision: frame,
            Width: width,
            Height: height,
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
            TimeSeconds: frame / 60f,
            PreviousFrame: frameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(
                RequireGpuReadback: true,
                PreferGpuPresentation: false),
            Theme: NprRenderTheme.Light,
            ShowGrid: false,
            IncludeDebugFrame: false,
            DebugOverlay: DebugOverlayKind.None,
            DiagnosticsOptions: CreateBenchmarkDiagnosticsOptions(),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"GPU readback benchmark failed on frame {frame}: {result.Status}");
        }

        totalMs += result.Diagnostics.TotalMilliseconds;
        renderMs += result.Diagnostics.TotalMilliseconds;
        allocatedBytes += result.Diagnostics.AllocatedBytes;
        pipelineMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "NprPipeline.Execute")?.Milliseconds ?? 0;
        drawMs += result.Diagnostics.Timings.Where(t =>
            t.Name == "GpuStrokeDraw" ||
            t.Name == "GpuToneSurfaceDraw" ||
            t.Name == "GpuMeshWireframeDraw" ||
            t.Name == "GpuDebugOverlayDraw").Sum(t => t.Milliseconds);
        readbackMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuReadback")?.Milliseconds ?? 0;
    }

    var divisor = NumericMath.AtLeast(frames, 1);
    WriteLog(
        $"GPU readback benchmark: avgTotal={totalMs / divisor:0.00}ms ({NumericMath.FramesPerSecond(totalMs / divisor):0.0} FPS), " +
        $"avgRender={renderMs / divisor:0.00}ms, avgPipeline={pipelineMs / divisor:0.00}ms, " +
        $"avgGpuDraw={drawMs / divisor:0.00}ms, avgGpuReadback={readbackMs / divisor:0.00}ms, " +
        $"avgAlloc={allocatedBytes / divisor / (1024.0 * 1024.0):0.00}MB.");
}

static void BenchRenderGrid(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);
    var frames = TryParsePositiveInt(args, 3, 30);
    var optimizerMode = ResolveRenderOptimizerMode(args);

    WriteLog($"Benchmarking CPU grid render: size={width}x{height}, frames={frames}.");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    double totalMs = 0;
    double renderMs = 0;
    double rasterMs = 0;
    long allocatedBytes = 0;
    var paths = 0;

    for (var frame = 1; frame <= frames; frame++)
    {
        var request = new NprRenderRequest(
            Revision: frame,
            Width: width,
            Height: height,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
            ContentKind: NprRenderContentKind.MeshWireframe,
            Scene: engine.Scene,
            Assets: assets,
            Camera: camera.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: entityStyles,
            Analysis: analysis,
            FrameHistoryState: frameHistory,
            Pipeline: null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: frameHistory.PeekNextFrameId(),
            TimeSeconds: frame / 60f,
            PreviousFrame: frameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(),
            Theme: NprRenderTheme.Light,
            ShowGrid: true,
            IncludeDebugFrame: false,
            DebugOverlay: DebugOverlayKind.None,
            DiagnosticsOptions: CreateBenchmarkDiagnosticsOptions(),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"CPU grid benchmark failed on frame {frame}: {result.Status}");
        }

        totalMs += result.Diagnostics.TotalMilliseconds;
        renderMs += result.Diagnostics.TotalMilliseconds;
        allocatedBytes += result.Diagnostics.AllocatedBytes;
        rasterMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuRasterize")?.Milliseconds ?? 0;
        paths = result.Diagnostics.PathCount;
    }

    var divisor = NumericMath.AtLeast(frames, 1);
    WriteLog(
        $"CPU grid render benchmark: avgTotal={totalMs / divisor:0.00}ms ({NumericMath.FramesPerSecond(totalMs / divisor):0.0} FPS), " +
        $"avgRender={renderMs / divisor:0.00}ms, avgRaster={rasterMs / divisor:0.00}ms, " +
        $"avgAlloc={allocatedBytes / divisor / (1024.0 * 1024.0):0.00}MB, paths={paths}.");
}

static void BenchRenderAlloc(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);
    var frames = TryParsePositiveInt(args, 3, 12);
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var mode = args.Length > 4 && !string.IsNullOrWhiteSpace(args[4])
        ? args[4]
        : "default";

    var contentKind = string.Equals(mode, "mesh", StringComparison.OrdinalIgnoreCase)
        ? NprRenderContentKind.MeshWireframe
        : NprRenderContentKind.NprPipeline;

    WriteLog($"Benchmarking render alloc: mode={mode}, size={width}x{height}, frames={frames}.");

    var engine = StfuRuntimeBootstrap.CreateEngine();
    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    if (contentKind == NprRenderContentKind.NprPipeline)
    {
        presetState.ApplyPreset(string.Equals(mode, "comic", StringComparison.OrdinalIgnoreCase) ? "comic-surface" : "default");
        frameHistory.Reset();
    }

    double totalMs = 0;
    double renderMs = 0;
    double pipelineMs = 0;
    double rasterMs = 0;
    long allocatedBytes = 0;
    var gen0Before = GC.CollectionCount(0);
    var gen1Before = GC.CollectionCount(1);
    var gen2Before = GC.CollectionCount(2);
    var paths = 0;
    var stepAllocTotals = new Dictionary<string, long>(StringComparer.Ordinal);

    for (var frame = 1; frame <= frames; frame++)
    {
        var request = new NprRenderRequest(
            Revision: frame,
            Width: width,
            Height: height,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
            ContentKind: contentKind,
            Scene: engine.Scene,
            Assets: assets,
            Camera: camera.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: entityStyles,
            Analysis: analysis,
            FrameHistoryState: frameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: frameHistory.PeekNextFrameId(),
            TimeSeconds: frame / 60f,
            PreviousFrame: frameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(),
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: false,
            DebugOverlay: DebugOverlayKind.None,
            DiagnosticsOptions: CreateBenchmarkDiagnosticsOptions(),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"Render alloc benchmark failed on frame {frame}: {result.Status}");
        }

        totalMs += result.Diagnostics.TotalMilliseconds;
        renderMs += result.Diagnostics.TotalMilliseconds;
        allocatedBytes += result.Diagnostics.AllocatedBytes;
        pipelineMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "NprPipeline.Execute")?.Milliseconds ?? 0;
        rasterMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuRasterize")?.Milliseconds ?? 0;
        foreach (var timing in result.Diagnostics.Timings.Where(timing => timing.Name.StartsWith("NprStep.", StringComparison.Ordinal)))
        {
            if (TryReadStepAllocation(timing.Notes, out var stepAllocatedBytes))
            {
                stepAllocTotals[timing.Name] = stepAllocTotals.GetValueOrDefault(timing.Name) + stepAllocatedBytes;
            }
        }

        paths = result.Diagnostics.PathCount;
    }

    var divisor = NumericMath.AtLeast(frames, 1);
    var gen0Delta = GC.CollectionCount(0) - gen0Before;
    var gen1Delta = GC.CollectionCount(1) - gen1Before;
    var gen2Delta = GC.CollectionCount(2) - gen2Before;
    var gcInfo = GC.GetGCMemoryInfo();
    var lohMb = TryGetGenerationSizeMb(gcInfo, 3);
    var pohMb = TryGetGenerationSizeMb(gcInfo, 4);
    WriteLog(
        $"Render alloc benchmark: avgTotal={totalMs / divisor:0.00}ms ({NumericMath.FramesPerSecond(totalMs / divisor):0.0} FPS), " +
        $"avgRender={renderMs / divisor:0.00}ms, avgPipeline={pipelineMs / divisor:0.00}ms, " +
        $"avgRaster={rasterMs / divisor:0.00}ms, avgAlloc={allocatedBytes / divisor / (1024.0 * 1024.0):0.00}MB, paths={paths}, " +
        $"gen0Delta={gen0Delta}, gen1Delta={gen1Delta}, gen2Delta={gen2Delta}, " +
        $"heapMb={gcInfo.HeapSizeBytes / (1024.0 * 1024.0):0.00}, fragmentedMb={gcInfo.FragmentedBytes / (1024.0 * 1024.0):0.00}, " +
        $"memoryLoadMb={gcInfo.MemoryLoadBytes / (1024.0 * 1024.0):0.00}, availableMb={gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0):0.00}, " +
        $"highLoadMb={gcInfo.HighMemoryLoadThresholdBytes / (1024.0 * 1024.0):0.00}, lohMb={FormatOptionalMb(lohMb)}, pohMb={FormatOptionalMb(pohMb)}.");

    if (stepAllocTotals.Count > 0)
    {
        var topAllocSteps = stepAllocTotals
            .OrderByDescending(pair => pair.Value)
            .Take(8)
            .Select(pair => $"{pair.Key["NprStep.".Length..]}={pair.Value / divisor / (1024.0 * 1024.0):0.00}MB");
        WriteLog($"Top alloc steps: {string.Join(", ", topAllocSteps)}.");
    }
}

static void BenchRenderResize(string[] args)
{
    var path = args.Length > 1 ? args[1] : Path.Combine("assets", "walking.fbx");
    var frames = TryParsePositiveInt(args, 2, 60);
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var mode = args.Length > 3 && !string.IsNullOrWhiteSpace(args[3])
        ? args[3]
        : "mesh";
    var contentKind = string.Equals(mode, "mesh", StringComparison.OrdinalIgnoreCase)
        ? NprRenderContentKind.MeshWireframe
        : NprRenderContentKind.NprPipeline;
    var fullPath = Path.GetFullPath(path);

    WriteLog($"Benchmarking render resize: asset={fullPath}, frames={frames}, mode={mode}.");

    var session = new UiEngineSession(StfuRuntimeBootstrap.CreateEngine());
    session.Workspace.Assets.SelectAssetCandidate(fullPath, "ResizeBench");
    session.Workspace.Assets.LoadAssetCommand.Execute(null);

    var renderer = session.Engine.Registry.GetRequired<INprRenderer>();
    var presetState = session.ActivePreset;
    if (contentKind == NprRenderContentKind.NprPipeline)
    {
        presetState.ApplyPreset(string.Equals(mode, "comic", StringComparison.OrdinalIgnoreCase) ? "comic-surface" : "default");
        session.FrameHistory.Reset();
    }

    var surfacePool = session.Engine.Registry.GetRequired<PixelSurfacePool>();
    var beforePool = surfacePool.Snapshot();
    var sizes = new (int Width, int Height)[]
    {
        (320, 180),
        (640, 360),
        (800, 600),
        (960, 540),
        (1280, 720)
    };

    double totalMs = 0;
    double renderMs = 0;
    long allocatedBytes = 0;
    var paths = 0;

    for (var frame = 1; frame <= frames; frame++)
    {
        var size = sizes[(frame - 1) % sizes.Length];
        var request = new NprRenderRequest(
            Revision: frame,
            Width: size.Width,
            Height: size.Height,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
            ContentKind: contentKind,
            Scene: session.Engine.Scene,
            Assets: session.Assets,
            Camera: session.CameraRig.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: session.EntityStyles,
            Analysis: session.Analysis,
            FrameHistoryState: session.FrameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: session.FrameHistory.PeekNextFrameId(),
            TimeSeconds: frame / 60f,
            PreviousFrame: session.FrameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(),
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: false,
            DebugOverlay: DebugOverlayKind.None,
            DiagnosticsOptions: CreateBenchmarkDiagnosticsOptions(),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"Render resize benchmark failed on frame {frame}: {result.Status}");
        }

        totalMs += result.Diagnostics.TotalMilliseconds;
        renderMs += result.Diagnostics.TotalMilliseconds;
        allocatedBytes += result.Diagnostics.AllocatedBytes;
        paths = result.Diagnostics.PathCount;
    }

    var afterPool = surfacePool.Snapshot();
    var divisor = NumericMath.AtLeast(frames, 1);
    WriteLog(
        $"Render resize benchmark: avgTotal={totalMs / divisor:0.00}ms ({NumericMath.FramesPerSecond(totalMs / divisor):0.0} FPS), " +
        $"avgRender={renderMs / divisor:0.00}ms, avgAlloc={allocatedBytes / divisor / (1024.0 * 1024.0):0.00}MB, paths={paths}.");
    WriteLog(
        $"PixelSurfacePool delta: created={afterPool.CreatedCount - beforePool.CreatedCount}, " +
        $"reused={afterPool.ReusedCount - beforePool.ReusedCount}, returned={afterPool.ReturnedCount - beforePool.ReturnedCount}, " +
        $"discarded={afterPool.DiscardedCount - beforePool.DiscardedCount}, retained={afterPool.RetainedCount}, " +
        $"retainedMB={afterPool.RetainedBytes / (1024.0 * 1024.0):0.00}.");
}

static void BenchRenderProfiles(string[] args)
{
    if (args.Length < 6)
    {
        throw new InvalidOperationException(
            "Usage: --bench-render-profiles <asset> <width> <height> <frames> <mode> [warmupFrames] [--workers n] [--worker-budget-mode mode] [--tile-size n] [--mesh-wireframe-topology raw|welded] [--animation wall-clock|fixed-step|off] [--animation-cache-warmup seconds]");
    }

    var path = Path.GetFullPath(args[1]);
    var width = TryParsePositiveInt(args, 2, 800);
    var height = TryParsePositiveInt(args, 3, 600);
    var frames = TryParsePositiveInt(args, 4, 12);
    var mode = args[5];
    var warmupFrames = args.Length > 6 && !args[6].StartsWith("--", StringComparison.Ordinal)
        ? TryParsePositiveInt(args, 6, 2)
        : 2;
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var meshTopologyMode = ResolveMeshWireframeTopologyMode(args);
    var gpuMeshPath = ResolveGpuMeshWireframePath(args);
    var animationMode = ResolveBenchmarkAnimationMode(args);
    var animationCacheWarmupSeconds = ResolveAnimationCacheWarmupSeconds(args);
    var workerCount = ResolveOptionalWorkerCount(args);
    var workerBudgetMode = ResolveWorkerBudgetMode(args);
    var tileSize = ResolveOptionalTileSize(args);
    var quality = NprQualityProfile.Default with
    {
        MeshWireframeTopologyMode = meshTopologyMode,
        GpuMeshWireframePath = gpuMeshPath
    };
    var contentKind = string.Equals(mode, "mesh", StringComparison.OrdinalIgnoreCase)
        ? NprRenderContentKind.MeshWireframe
        : NprRenderContentKind.NprPipeline;

    WriteLog(
        $"Benchmarking render profiles: asset={path}, mode={mode}, size={width}x{height}, " +
        $"warmup={warmupFrames}, frames={frames}, meshTopology={meshTopologyMode}, " +
        $"gpuMeshPath={gpuMeshPath}, animation={animationMode}, animationCacheWarmup={animationCacheWarmupSeconds:0.00}s, " +
        $"workerBudgetMode={workerBudgetMode}, maxWorkers={workerCount}, tileSize={tileSize}.");

    var cpu = MeasureProfile(
        "cpu",
        NprExecutionProfile.FullCpuReference,
        requireGpuReadback: false,
        preferGpuPresentation: false);
    if (cpu is { } cpuResult)
    {
        WriteProfile(cpuResult);
    }

    var gpuDirect = MeasureProfile(
        "cpu-gpu-direct",
        NprExecutionProfile.CpuDrivenGpuAccelerated,
        requireGpuReadback: false,
        preferGpuPresentation: true);
    if (gpuDirect is { } direct)
    {
        WriteProfile(direct);
    }

    var gpuReadback = MeasureProfile(
        "cpu-gpu-readback",
        NprExecutionProfile.CpuDrivenGpuAccelerated,
        requireGpuReadback: true,
        preferGpuPresentation: false);
    if (gpuReadback is { } readback)
    {
        WriteProfile(readback);
    }

    (
        string Label,
        double TotalMs,
        double AnimationMs,
        double RenderMs,
        double PipelineMs,
        double CpuMeshMs,
        double CpuRasterMs,
        double GpuMeshMs,
        double GpuStrokeBuildMs,
        double GpuUploadMs,
        double GpuDrawMs,
        double GpuReadbackMs,
        long AllocatedBytes,
        int Edges,
        int Paths)? MeasureProfile(
            string label,
            NprExecutionProfile profile,
            bool requireGpuReadback,
            bool preferGpuPresentation)
    {
        var session = new UiEngineSession(StfuRuntimeBootstrap.CreateEngine());
        if (profile == NprExecutionProfile.CpuDrivenGpuAccelerated &&
            (!session.Engine.Registry.TryGet<IGpuRenderBackend>(out var gpu) || !gpu.IsAvailable))
        {
            WriteLog($"Render profile '{label}' skipped: GPU backend is not available.");
            return null;
        }

        session.Workspace.Assets.SelectAssetCandidate(path, $"ProfileBench-{label}");
        session.Workspace.Assets.LoadAssetCommand.Execute(null);
        WarmAnimationCache(session, animationCacheWarmupSeconds);

        var presetState = session.ActivePreset;
        if (contentKind == NprRenderContentKind.NprPipeline)
        {
            presetState.ApplyPreset(string.Equals(mode, "comic", StringComparison.OrdinalIgnoreCase) ? "comic-surface" : "default");
            session.FrameHistory.Reset();
        }

        var renderer = session.Engine.Registry.GetRequired<INprRenderer>();
        var budget = profile == NprExecutionProfile.CpuDrivenGpuAccelerated
            ? new NprFrameBudget(
                MaxWorkerThreads: workerCount,
                TileSize: tileSize,
                RequireGpuReadback: requireGpuReadback,
                AllowGpuReadback: requireGpuReadback,
                PreferGpuPresentation: preferGpuPresentation,
                WorkerBudgetMode: workerBudgetMode)
            : new NprFrameBudget(
                MaxWorkerThreads: workerCount,
                TileSize: tileSize,
                WorkerBudgetMode: workerBudgetMode);
        WriteLog(
            $"Render profile '{label}' worker budget: mode={budget.WorkerBudgetMode}, " +
            $"maxWorkers={budget.MaxWorkerThreads}, resolvedWorkers={budget.ResolveWorkerCount()}.");

        double totalMs = 0;
        double animationMs = 0;
        double renderMs = 0;
        double pipelineMs = 0;
        double cpuMeshMs = 0;
        double cpuRasterMs = 0;
        double gpuMeshMs = 0;
        double gpuStrokeBuildMs = 0;
        double gpuUploadMs = 0;
        double gpuDrawMs = 0;
        double gpuReadbackMs = 0;
        long allocatedBytes = 0;
        var edges = 0;
        var paths = 0;

        var totalFrameCount = warmupFrames + frames;
        for (var frame = 1; frame <= totalFrameCount; frame++)
        {
            var totalWatch = Stopwatch.StartNew();

            var animationWatch = Stopwatch.StartNew();
            TickBenchmarkAnimation(session, animationMode, frame);
            animationWatch.Stop();
            var animationElapsed = animationWatch.Elapsed.TotalMilliseconds;

            var request = new NprRenderRequest(
                Revision: frame,
                Width: width,
                Height: height,
                ExecutionProfile: profile,
                ContentKind: contentKind,
                Scene: session.Engine.Scene,
                Assets: session.Assets,
                Camera: session.CameraRig.Camera,
                Settings: presetState.ActiveSettings,
                Style: presetState.ActiveGrammar,
                StyleSet: presetState.ActiveStyleSet,
                EntityStyles: session.EntityStyles,
                Analysis: session.Analysis,
                FrameHistoryState: session.FrameHistory,
                Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
                ActivePresetId: presetState.ActivePreset.Metadata.Id,
                ActivePipelineId: presetState.ActivePreset.PipelineId,
                FrameId: frame,
                TimeSeconds: frame / 60f,
                PreviousFrame: contentKind == NprRenderContentKind.NprPipeline ? session.FrameHistory.GetPreviousFrame() : null,
                Quality: quality,
                Budget: budget,
                Theme: NprRenderTheme.Light,
                ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
                IncludeDebugFrame: false,
                DebugOverlay: DebugOverlayKind.None,
                DiagnosticsOptions: CreateBenchmarkDiagnosticsOptions(),
                OptimizerMode: optimizerMode);

            using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
            totalWatch.Stop();

            if (result.Status != NprRenderStatus.Completed)
            {
                throw new InvalidOperationException($"Render profile '{label}' failed on frame {frame}: {result.Status}");
            }

            if (frame <= warmupFrames)
            {
                continue;
            }

            totalMs += totalWatch.Elapsed.TotalMilliseconds;
            animationMs += animationElapsed;
            renderMs += result.Diagnostics.TotalMilliseconds;
            allocatedBytes += result.Diagnostics.AllocatedBytes;
            pipelineMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "NprPipeline.Execute")?.Milliseconds ?? 0;
            cpuMeshMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuMeshWireframe")?.Milliseconds ?? 0;
            cpuRasterMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuRasterize")?.Milliseconds ?? 0;
            gpuMeshMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuMeshBuild")?.Milliseconds ?? 0;
            gpuStrokeBuildMs += result.Diagnostics.Timings.Where(t => t.Name == "GpuStrokeBuild").Sum(t => t.Milliseconds);
            gpuUploadMs += result.Diagnostics.Timings.Where(t =>
                t.Name == "GpuStrokeUpload" ||
                t.Name == "GpuMeshUpload").Sum(t => t.Milliseconds);
            gpuDrawMs += result.Diagnostics.Timings.Where(t =>
                t.Name == "GpuStrokeDraw" ||
                t.Name == "GpuToneSurfaceDraw" ||
                t.Name == "GpuMeshWireframeDraw" ||
                t.Name == "GpuDebugOverlayDraw").Sum(t => t.Milliseconds);
            gpuReadbackMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuReadback")?.Milliseconds ?? 0;
            var meshTiming = profile == NprExecutionProfile.FullCpuReference
                ? result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuMeshWireframe")
                : result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuMeshBuild");
            if (TryReadTimingNoteInt(meshTiming?.Notes, "edges", out var edgeCount))
            {
                edges = edgeCount;
            }

            paths = result.Diagnostics.PathCount;
        }

        var divisor = NumericMath.AtLeast(frames, 1);
        return (
            label,
            totalMs / divisor,
            animationMs / divisor,
            renderMs / divisor,
            pipelineMs / divisor,
            cpuMeshMs / divisor,
            cpuRasterMs / divisor,
            gpuMeshMs / divisor,
            gpuStrokeBuildMs / divisor,
            gpuUploadMs / divisor,
            gpuDrawMs / divisor,
            gpuReadbackMs / divisor,
            allocatedBytes / divisor,
            edges,
            paths);
    }

    void WriteProfile(
        (
            string Label,
            double TotalMs,
            double AnimationMs,
            double RenderMs,
            double PipelineMs,
            double CpuMeshMs,
            double CpuRasterMs,
            double GpuMeshMs,
            double GpuStrokeBuildMs,
            double GpuUploadMs,
            double GpuDrawMs,
            double GpuReadbackMs,
            long AllocatedBytes,
            int Edges,
            int Paths) result)
    {
        WriteLog(
            $"Render profile '{result.Label}': avgTotal={result.TotalMs:0.00}ms ({NumericMath.FramesPerSecond(result.TotalMs):0.0} FPS), " +
            $"avgAnimation={result.AnimationMs:0.00}ms, avgRender={result.RenderMs:0.00}ms, avgPipeline={result.PipelineMs:0.00}ms, " +
            $"avgCpuMesh={result.CpuMeshMs:0.00}ms, avgCpuRaster={result.CpuRasterMs:0.00}ms, " +
            $"avgGpuMesh={result.GpuMeshMs:0.00}ms, avgGpuStrokeBuild={result.GpuStrokeBuildMs:0.00}ms, " +
            $"avgGpuUpload={result.GpuUploadMs:0.00}ms, avgGpuDraw={result.GpuDrawMs:0.00}ms, avgGpuReadback={result.GpuReadbackMs:0.00}ms, " +
            $"avgAlloc={result.AllocatedBytes / (1024.0 * 1024.0):0.00}MB, edges={result.Edges}, paths={result.Paths}.");
    }
}

static void SmokeFbxPlayback(string[] args)
{
    var path = args.Length > 1 ? args[1] : Path.Combine("assets", "walking.fbx");
    var fullPath = Path.GetFullPath(path);

    WriteLog($"Running FBX playback smoke: {fullPath}");
    using var sampler = FbxBakedAnimationSampler.Load(fullPath);
    if (sampler.MeshCount <= 0)
    {
        throw new InvalidOperationException("FBX playback smoke found no meshes.");
    }

    if (sampler.Animations.Count <= 0)
    {
        throw new InvalidOperationException("FBX playback smoke found no animations.");
    }

    var first = sampler.BakeCombinedMesh(animationIndex: 0, timeSeconds: 0);
    var firstSamples = CapturePositionSamples(first);
    var second = sampler.BakeCombinedMesh(animationIndex: 0, timeSeconds: 0.5);
    if (first.Vertices.Count == 0 || first.Triangles.Count == 0 ||
        second.Vertices.Count == 0 || second.Triangles.Count == 0)
    {
        throw new InvalidOperationException("FBX playback smoke produced an empty baked mesh.");
    }

    var delta = CalculateSampleDelta(firstSamples, second);
    if (delta <= 0.0001f)
    {
        throw new InvalidOperationException("FBX playback smoke did not observe vertex motion.");
    }

    WriteLog(
        $"FBX playback smoke passed: meshes={sampler.MeshCount}, animations={sampler.Animations.Count}, " +
        $"vertices={second.Vertices.Count}, triangles={second.Triangles.Count}, sampleDelta={delta:0.0000}.");
}

static void SmokeFbxUiLoad(string[] args)
{
    var path = args.Length > 1 ? args[1] : Path.Combine("assets", "walking.fbx");
    var fullPath = Path.GetFullPath(path);

    WriteLog($"Running FBX UI load smoke: {fullPath}");
    var session = new UiEngineSession(StfuRuntimeBootstrap.CreateEngine());
    session.Workspace.Assets.SelectAssetCandidate(fullPath, "Smoke");
    session.Workspace.Assets.LoadAssetCommand.Execute(null);

    var entry = session.Assets.MeshEntries.FirstOrDefault(candidate =>
        string.Equals(candidate.Path, fullPath, StringComparison.OrdinalIgnoreCase));
    if (entry is null)
    {
        throw new InvalidOperationException("FBX UI load smoke did not add the FBX mesh to AssetRegistry.");
    }

    var entity = session.Engine.Scene.Entities.FirstOrDefault(candidate => candidate.Mesh == entry.Handle);
    if (entity is null)
    {
        throw new InvalidOperationException("FBX UI load smoke did not assign the FBX mesh to an entity.");
    }

    var before = entry.Mesh;
    var beforeSamples = CapturePositionSamples(before);
    Thread.Sleep(50);
    session.Workspace.Assets.TickAnimation();
    if (!session.Assets.TryGetMesh(entry.Handle, out var after))
    {
        throw new InvalidOperationException("FBX UI load smoke lost the animated mesh handle.");
    }

    var delta = CalculateSampleDelta(beforeSamples, after);
    if (delta <= 0.0001f)
    {
        throw new InvalidOperationException("FBX UI load smoke did not observe animated mesh replacement.");
    }

    WriteLog(
        $"FBX UI load smoke passed: handle={entry.Handle.Value}, entity={entity.Name}, " +
        $"vertices={after.Vertices.Count}, triangles={after.Triangles.Count}, sampleDelta={delta:0.0000}.");
}

static void SmokeFbxAbi(string[] args)
{
    var path = args.Length > 1 ? args[1] : Path.Combine("assets", "walking.fbx");
    var fullPath = Path.GetFullPath(path);

    WriteLog($"Running FBX ABI smoke: {fullPath}");
    var result = FbxNativeAbiSmoke.Verify(fullPath);
    WriteLog(
        $"FBX ABI smoke passed: vertexStructBytes={result.VertexStructBytes}, " +
        $"vertices={result.VertexCount}, triangles={result.TriangleCount}, " +
        $"logicalIds={result.LogicalVertexIdCount}, distinctLogicalIds={result.DistinctLogicalVertexIdCount}.");
}

static void BenchFbxMeshRender(string[] args)
{
    var path = args.Length > 1 ? args[1] : Path.Combine("assets", "walking.fbx");
    var width = TryParsePositiveInt(args, 2, 800);
    var height = TryParsePositiveInt(args, 3, 600);
    var frameCount = TryParsePositiveInt(args, 4, 12);
    var tileSize = TryParsePositiveInt(args, 5, 32);
    var legacyWorkerCount = args.Length > 6 && int.TryParse(args[6], out var workers) ? workers : 0;
    var workerCount = ResolveOptionalWorkerCount(args, legacyWorkerCount);
    var workerBudgetMode = ResolveWorkerBudgetMode(args);
    var optimizerMode = ResolveRenderOptimizerMode(args);
    var meshTopologyMode = ResolveMeshWireframeTopologyMode(args);
    var gpuMeshPath = ResolveGpuMeshWireframePath(args);
    var animationMode = ResolveBenchmarkAnimationMode(args);
    var animationCacheWarmupSeconds = ResolveAnimationCacheWarmupSeconds(args);
    var quality = NprQualityProfile.Default with
    {
        MeshWireframeTopologyMode = meshTopologyMode,
        GpuMeshWireframePath = gpuMeshPath
    };
    var mode = args.Length > 7 && !args[7].StartsWith("--", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(args[7])
        ? args[7]
        : "mesh";
    var contentKind = string.Equals(mode, "mesh", StringComparison.OrdinalIgnoreCase)
        ? NprRenderContentKind.MeshWireframe
        : NprRenderContentKind.NprPipeline;
    var fullPath = Path.GetFullPath(path);

    WriteLog(
        $"Benchmarking FBX {mode} render: {fullPath}, size={width}x{height}, frames={frameCount}, " +
        $"tile={tileSize}, workers={workerCount}, meshTopology={meshTopologyMode}, " +
        $"gpuMeshPath={gpuMeshPath}, animation={animationMode}, animationCacheWarmup={animationCacheWarmupSeconds:0.00}s, " +
        $"workerBudgetMode={workerBudgetMode}.");

    var session = new UiEngineSession(StfuRuntimeBootstrap.CreateEngine());
    session.Workspace.Assets.SelectAssetCandidate(fullPath, "Bench");
    session.Workspace.Assets.LoadAssetCommand.Execute(null);
    WarmAnimationCache(session, animationCacheWarmupSeconds);

    var renderer = session.Engine.Registry.GetRequired<INprRenderer>();
    var presetState = session.ActivePreset;
    if (contentKind == NprRenderContentKind.NprPipeline)
    {
        presetState.ApplyPreset(string.Equals(mode, "comic", StringComparison.OrdinalIgnoreCase) ? "comic-surface" : "default");
    }

    var totalMs = 0.0;
    var animationMs = 0.0;
    var renderMs = 0.0;
    var pipelineMs = 0.0;
    var meshMs = 0.0;
    var rasterMs = 0.0;
    var allocatedBytes = 0L;
    var edges = 0;
    var paths = 0;
    var stepTotals = new Dictionary<string, double>(StringComparer.Ordinal);
    var stepAllocTotals = new Dictionary<string, long>(StringComparer.Ordinal);
    var budget = new NprFrameBudget(
        MaxWorkerThreads: workerCount,
        TileSize: tileSize,
        WorkerBudgetMode: workerBudgetMode);
    WriteLog(
        $"FBX {mode} worker budget: mode={budget.WorkerBudgetMode}, " +
        $"maxWorkers={budget.MaxWorkerThreads}, resolvedWorkers={budget.ResolveWorkerCount()}.");

    for (var frame = 1; frame <= frameCount; frame++)
    {
        var totalWatch = Stopwatch.StartNew();

        var animationWatch = Stopwatch.StartNew();
        TickBenchmarkAnimation(session, animationMode, frame);
        animationWatch.Stop();
        animationMs += animationWatch.Elapsed.TotalMilliseconds;

        var request = new NprRenderRequest(
            Revision: frame,
            Width: width,
            Height: height,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
            ContentKind: contentKind,
            Scene: session.Engine.Scene,
            Assets: session.Assets,
            Camera: session.CameraRig.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: session.EntityStyles,
            Analysis: session.Analysis,
            FrameHistoryState: session.FrameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: frame,
            TimeSeconds: frame / 60f,
            PreviousFrame: null,
            Quality: quality,
            Budget: budget,
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: false,
            DiagnosticsOptions: CreateBenchmarkDiagnosticsOptions(),
            OptimizerMode: optimizerMode);

        using var result = renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        totalWatch.Stop();

        if (result.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"FBX mesh render benchmark failed on frame {frame}: {result.Status}");
        }

        totalMs += totalWatch.Elapsed.TotalMilliseconds;
        renderMs += result.Diagnostics.TotalMilliseconds;
        allocatedBytes += result.Diagnostics.AllocatedBytes;
        pipelineMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "NprPipeline.Execute")?.Milliseconds ?? 0;
        meshMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuMeshWireframe")?.Milliseconds ?? 0;
        rasterMs += result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuRasterize")?.Milliseconds ?? 0;
        var meshTiming = result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuMeshWireframe");
        if (TryReadTimingNoteInt(meshTiming?.Notes, "edges", out var edgeCount))
        {
            edges = edgeCount;
        }

        foreach (var timing in result.Diagnostics.Timings.Where(timing => timing.Name.StartsWith("NprStep.", StringComparison.Ordinal)))
        {
            stepTotals[timing.Name] = stepTotals.GetValueOrDefault(timing.Name) + timing.Milliseconds;
            if (TryReadStepAllocation(timing.Notes, out var stepAllocatedBytes))
            {
                stepAllocTotals[timing.Name] = stepAllocTotals.GetValueOrDefault(timing.Name) + stepAllocatedBytes;
            }
        }

        paths = result.Diagnostics.PathCount;
    }

    var divisor = NumericMath.AtLeast(frameCount, 1);
    WriteLog(
        $"FBX {mode} render benchmark: avgTotal={totalMs / divisor:0.00}ms ({NumericMath.FramesPerSecond(totalMs / divisor):0.0} FPS), " +
        $"avgAnimation={animationMs / divisor:0.00}ms, avgRender={renderMs / divisor:0.00}ms, " +
        $"avgPipeline={pipelineMs / divisor:0.00}ms, avgMesh={meshMs / divisor:0.00}ms, " +
        $"avgRaster={rasterMs / divisor:0.00}ms, avgAlloc={allocatedBytes / divisor / (1024.0 * 1024.0):0.00}MB, " +
        $"edges={edges}, paths={paths}.");

    if (stepTotals.Count > 0)
    {
        var topSteps = stepTotals
            .OrderByDescending(pair => pair.Value)
            .Take(6)
            .Select(pair => $"{pair.Key["NprStep.".Length..]}={pair.Value / divisor:0.00}ms");
        WriteLog($"Top NPR steps: {string.Join(", ", topSteps)}.");
    }

    if (stepAllocTotals.Count > 0)
    {
        var topAllocSteps = stepAllocTotals
            .OrderByDescending(pair => pair.Value)
            .Take(6)
            .Select(pair => $"{pair.Key["NprStep.".Length..]}={pair.Value / divisor / (1024.0 * 1024.0):0.00}MB");
        WriteLog($"Top NPR alloc: {string.Join(", ", topAllocSteps)}.");
    }
}

static bool TryReadStepAllocation(string? notes, out long allocatedBytes)
{
    allocatedBytes = 0;
    if (string.IsNullOrEmpty(notes))
    {
        return false;
    }

    const string marker = "alloc=";
    var index = notes.LastIndexOf(marker, StringComparison.Ordinal);
    if (index < 0)
    {
        return false;
    }

    var start = index + marker.Length;
    var end = start;
    while (end < notes.Length && char.IsDigit(notes[end]))
    {
        end++;
    }

    return end > start &&
        long.TryParse(notes.AsSpan(start, end - start), out allocatedBytes);
}

static bool TryReadTimingNoteInt(string? notes, string key, out int value)
{
    value = 0;
    if (string.IsNullOrEmpty(notes))
    {
        return false;
    }

    var marker = key + "=";
    var index = notes.IndexOf(marker, StringComparison.Ordinal);
    if (index < 0)
    {
        return false;
    }

    var start = index + marker.Length;
    var end = start;
    while (end < notes.Length && char.IsDigit(notes[end]))
    {
        end++;
    }

    return end > start && int.TryParse(notes.AsSpan(start, end - start), out value);
}

static int ResolveOptionalWorkerCount(string[] args, int fallback = 0)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--workers", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(args[i], "--render-workers", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var workerCount) &&
            workerCount >= 0)
        {
            return workerCount;
        }

        throw new InvalidOperationException($"{args[i]} expects a non-negative integer.");
    }

    return NumericMath.AtLeast(fallback, 0);
}

static int ResolveOptionalTileSize(string[] args, int fallback = 32)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--tile-size", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(args[i], "--render-tile-size", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tileSize) &&
            tileSize > 0)
        {
            return tileSize;
        }

        throw new InvalidOperationException($"{args[i]} expects a positive integer.");
    }

    return NumericMath.AtLeast(fallback, 1);
}

static WorkerBudgetMode ResolveWorkerBudgetMode(string[] args, WorkerBudgetMode fallback = WorkerBudgetMode.Performance)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--worker-budget-mode", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(args[i], "--worker-mode", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return args[i + 1].ToLowerInvariant().Replace('_', '-') switch
        {
            "balanced" => WorkerBudgetMode.Balanced,
            "performance" or "perf" => WorkerBudgetMode.Performance,
            "max" or "max-performance" or "maxperformance" => WorkerBudgetMode.MaxPerformance,
            "benchmark" or "bench" => WorkerBudgetMode.Benchmark,
            "background" or "background-safe" or "backgroundsafe" => WorkerBudgetMode.BackgroundSafe,
            "single" or "single-thread" or "single-thread-deterministic" or "deterministic" => WorkerBudgetMode.SingleThreadDeterministic,
            var value => throw new InvalidOperationException(
                $"Unsupported worker budget mode '{value}'. Use balanced, performance, max-performance, benchmark, background-safe, or single-thread-deterministic.")
        };
    }

    return fallback;
}

static double? TryGetGenerationSizeMb(GCMemoryInfo gcInfo, int generationIndex)
{
    var generations = gcInfo.GenerationInfo;
    if ((uint)generationIndex >= (uint)generations.Length)
    {
        return null;
    }

    return generations[generationIndex].SizeAfterBytes / (1024.0 * 1024.0);
}

static string FormatOptionalMb(double? value)
{
    return value.HasValue ? value.Value.ToString("0.00", CultureInfo.InvariantCulture) : "n/a";
}

static NprRenderOptimizerMode ResolveRenderOptimizerMode(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--render-optimizer", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return NprRenderOptimizerModeResolver.Parse(args[i + 1]);
    }

    return NprRenderOptimizerModeResolver.ResolveFromEnvironment();
}

static MeshWireframeTopologyMode ResolveMeshWireframeTopologyMode(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--mesh-wireframe-topology", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return args[i + 1].ToLowerInvariant() switch
        {
            "raw" => MeshWireframeTopologyMode.Raw,
            "welded" or "weld" or "quantized" => MeshWireframeTopologyMode.Welded,
            var value => throw new InvalidOperationException(
                $"Unsupported mesh wireframe topology '{value}'. Use raw or welded.")
        };
    }

    return NprQualityProfile.Default.MeshWireframeTopologyMode;
}

static GpuMeshWireframePath ResolveGpuMeshWireframePath(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--gpu-mesh-path", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return args[i + 1].ToLowerInvariant() switch
        {
            "native" => GpuMeshWireframePath.Native,
            "stroke" or "fallback" => GpuMeshWireframePath.Stroke,
            var value => throw new InvalidOperationException(
                $"Unsupported GPU mesh path '{value}'. Use native or stroke.")
        };
    }

    return NprQualityProfile.Default.GpuMeshWireframePath;
}

static BenchmarkAnimationMode ResolveBenchmarkAnimationMode(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--animation", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return args[i + 1].ToLowerInvariant() switch
        {
            "wall-clock" or "wallclock" or "tick" => BenchmarkAnimationMode.WallClock,
            "fixed-step" or "fixed" => BenchmarkAnimationMode.FixedStep,
            "off" or "none" => BenchmarkAnimationMode.Off,
            var value => throw new InvalidOperationException(
                $"Unsupported benchmark animation mode '{value}'. Use wall-clock, fixed-step, or off.")
        };
    }

    return BenchmarkAnimationMode.WallClock;
}

static double ResolveAnimationCacheWarmupSeconds(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!string.Equals(args[i], "--animation-cache-warmup", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) &&
            seconds >= 0)
        {
            return seconds;
        }

        throw new InvalidOperationException("--animation-cache-warmup expects a non-negative seconds value.");
    }

    return 0;
}

static void WarmAnimationCache(UiEngineSession session, double seconds)
{
    if (seconds <= 0)
    {
        return;
    }

    var watch = Stopwatch.StartNew();
    var completed = session.Workspace.Assets.WaitForAnimationCache(TimeSpan.FromSeconds(seconds));
    WriteLog(
        $"Animation cache warmup {(completed ? "completed" : "timed out")} after " +
        $"{watch.Elapsed.TotalMilliseconds:0.0}ms.");
}

static void TickBenchmarkAnimation(UiEngineSession session, BenchmarkAnimationMode mode, int frame)
{
    switch (mode)
    {
        case BenchmarkAnimationMode.WallClock:
            session.Workspace.Assets.TickAnimation();
            break;
        case BenchmarkAnimationMode.FixedStep:
            session.Workspace.Assets.BakeAnimationAtTime(frame / 60.0);
            break;
        case BenchmarkAnimationMode.Off:
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
    }
}

static void VerifyRenderParity(string[] args)
{
    if (args.Length < 5)
    {
        throw new InvalidOperationException(
            "Usage: --verify-render-parity <mode> <width> <height> <frames>");
    }

    var mode = args[1];
    var width = TryParsePositiveInt(args, 2, 800);
    var height = TryParsePositiveInt(args, 3, 600);
    var frames = TryParsePositiveInt(args, 4, 1);
    var scenario = ResolveParityScenario(mode);

    WriteLog(
        $"Verifying render parity: mode={scenario.Label}, preset={scenario.PresetId ?? "<none>"}, " +
        $"content={scenario.ContentKind}, size={width}x{height}, frames={frames}.");

    var baseline = CreateParityRunner(width, height, scenario, NprRenderOptimizerMode.Off);
    var optimized = CreateParityRunner(width, height, scenario, NprRenderOptimizerMode.On);

    for (var frame = 1; frame <= frames; frame++)
    {
        using var baselineResult = baseline.Render(frame);
        using var optimizedResult = optimized.Render(frame);

        if (baselineResult.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"Baseline render failed on frame {frame}: {baselineResult.Status}.");
        }

        if (optimizedResult.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"Optimized render failed on frame {frame}: {optimizedResult.Status}.");
        }

        var baselineSnapshot = CreateParitySnapshot(scenario, baselineResult, frame);
        var optimizedSnapshot = CreateParitySnapshot(scenario, optimizedResult, frame);

        if (baselineSnapshot.StrokeFrameHash != optimizedSnapshot.StrokeFrameHash)
        {
            throw new InvalidOperationException(
                $"Parity failed on frame {frame}: StrokeFrameHash mismatch " +
                $"baseline=0x{baselineSnapshot.StrokeFrameHash:X16} optimized=0x{optimizedSnapshot.StrokeFrameHash:X16}.");
        }

        if (baselineSnapshot.NprFrameHash != optimizedSnapshot.NprFrameHash)
        {
            throw new InvalidOperationException(
                $"Parity failed on frame {frame}: NprFrameHash mismatch " +
                $"baseline=0x{baselineSnapshot.NprFrameHash:X16} optimized=0x{optimizedSnapshot.NprFrameHash:X16}.");
        }

        if (baselineSnapshot.DebugFrameHash != optimizedSnapshot.DebugFrameHash)
        {
            WriteLog(
                $"Parity frame {frame} warning: DebugFrameHash mismatch " +
                $"baseline=0x{baselineSnapshot.DebugFrameHash:X16} optimized=0x{optimizedSnapshot.DebugFrameHash:X16}.");
        }

        if (baselineSnapshot.PixelHash != optimizedSnapshot.PixelHash)
        {
            var leftSurface = baselineResult.PixelSurfaceLease?.Surface
                ?? throw new InvalidOperationException("Baseline parity render did not produce PixelSurface.");
            var rightSurface = optimizedResult.PixelSurfaceLease?.Surface
                ?? throw new InvalidOperationException("Optimized parity render did not produce PixelSurface.");
            var diff = PixelSurfaceDiff.Compare(leftSurface, rightSurface, tolerance: 0);

            throw new InvalidOperationException(
                $"Parity failed on frame {frame}: PixelHash mismatch " +
                $"baseline=0x{baselineSnapshot.PixelHash:X16} optimized=0x{optimizedSnapshot.PixelHash:X16}; " +
                $"differentPixels={diff.DifferentPixelCount}, maxChannelDelta={diff.MaxChannelDelta}, " +
                $"firstDiff=({diff.FirstDifferentX},{diff.FirstDifferentY})/{diff.FirstDifferentChannel}.");
        }

        WriteLog(
            $"Parity frame {frame} ok: pixel=0x{baselineSnapshot.PixelHash:X16}, " +
            $"stroke=0x{baselineSnapshot.StrokeFrameHash:X16}, npr=0x{baselineSnapshot.NprFrameHash:X16}, " +
            $"paths={baselineSnapshot.PathCount}, layers={baselineSnapshot.LayerCount}, tones={baselineSnapshot.ToneSurfaceCount}.");
    }

    WriteLog("Render parity verification passed.");
}

static void VerifyGpuMeshParity(string[] args)
{
    if (args.Length < 5)
    {
        throw new InvalidOperationException(
            "Usage: --verify-gpu-mesh-parity <asset> <width> <height> <frames> [--mesh-wireframe-topology raw|welded] [--animation wall-clock|fixed-step|off] [--animation-cache-warmup seconds]");
    }

    var path = Path.GetFullPath(args[1]);
    var width = TryParsePositiveInt(args, 2, 800);
    var height = TryParsePositiveInt(args, 3, 600);
    var frames = TryParsePositiveInt(args, 4, 1);
    var meshTopologyMode = ResolveMeshWireframeTopologyMode(args);
    var animationMode = ResolveBenchmarkAnimationMode(args);
    var animationCacheWarmupSeconds = ResolveAnimationCacheWarmupSeconds(args);

    WriteLog(
        $"Verifying GPU mesh parity: asset={path}, size={width}x{height}, frames={frames}, " +
        $"meshTopology={meshTopologyMode}, animation={animationMode}, tolerance=1.");

    var nativeSession = CreateGpuMeshParitySession(path, animationCacheWarmupSeconds);
    var strokeSession = CreateGpuMeshParitySession(path, animationCacheWarmupSeconds);
    var nativeRenderer = nativeSession.Engine.Registry.GetRequired<INprRenderer>();
    var strokeRenderer = strokeSession.Engine.Registry.GetRequired<INprRenderer>();
    var nativeQuality = NprQualityProfile.Default with
    {
        MeshWireframeTopologyMode = meshTopologyMode,
        GpuMeshWireframePath = GpuMeshWireframePath.Native
    };
    var strokeQuality = NprQualityProfile.Default with
    {
        MeshWireframeTopologyMode = meshTopologyMode,
        GpuMeshWireframePath = GpuMeshWireframePath.Stroke
    };
    var budget = new NprFrameBudget(
        RequireGpuReadback: true,
        AllowGpuReadback: true,
        PreferGpuPresentation: false);

    for (var frame = 1; frame <= frames; frame++)
    {
        TickBenchmarkAnimation(nativeSession, animationMode, frame);
        TickBenchmarkAnimation(strokeSession, animationMode, frame);

        using var nativeResult = RenderGpuMeshParityFrame(nativeSession, nativeRenderer, width, height, frame, nativeQuality, budget);
        using var strokeResult = RenderGpuMeshParityFrame(strokeSession, strokeRenderer, width, height, frame, strokeQuality, budget);

        if (nativeResult.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"GPU native mesh parity render failed on frame {frame}: {nativeResult.Status}.");
        }

        if (strokeResult.Status != NprRenderStatus.Completed)
        {
            throw new InvalidOperationException($"GPU stroke mesh parity render failed on frame {frame}: {strokeResult.Status}.");
        }

        var nativeSurface = nativeResult.PixelSurfaceLease?.Surface
            ?? throw new InvalidOperationException("GPU native mesh parity did not produce PixelSurface.");
        var strokeSurface = strokeResult.PixelSurfaceLease?.Surface
            ?? throw new InvalidOperationException("GPU stroke mesh parity did not produce PixelSurface.");
        var diff = PixelSurfaceDiff.Compare(nativeSurface, strokeSurface, tolerance: 1);
        var nativeHash = NprRenderParityHasher.HashPixelSurface(nativeSurface);
        var strokeHash = NprRenderParityHasher.HashPixelSurface(strokeSurface);
        var nativeMeshTiming = nativeResult.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuMeshBuild");
        var strokeMeshTiming = strokeResult.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuMeshBuild");

        if (diff.DifferentPixelCount > 0)
        {
            var nativePixel = FormatPixelAt(nativeSurface, diff.FirstDifferentX, diff.FirstDifferentY);
            var strokePixel = FormatPixelAt(strokeSurface, diff.FirstDifferentX, diff.FirstDifferentY);
            throw new InvalidOperationException(
                $"GPU mesh parity failed on frame {frame}: native=0x{nativeHash:X16} stroke=0x{strokeHash:X16}; " +
                $"differentPixels={diff.DifferentPixelCount}, maxChannelDelta={diff.MaxChannelDelta}, " +
                $"firstDiff=({diff.FirstDifferentX},{diff.FirstDifferentY})/{diff.FirstDifferentChannel}, " +
                $"nativePixel={nativePixel}, strokePixel={strokePixel}; " +
                $"nativeNotes={nativeMeshTiming?.Notes ?? "<none>"}; strokeNotes={strokeMeshTiming?.Notes ?? "<none>"}.");
        }

        WriteLog(
            $"GPU mesh parity frame {frame} ok: native=0x{nativeHash:X16}, stroke=0x{strokeHash:X16}, " +
            $"nativePaths={nativeResult.Diagnostics.PathCount}, strokePaths={strokeResult.Diagnostics.PathCount}, " +
            $"nativeMesh={nativeMeshTiming?.Milliseconds ?? 0:0.###}ms, strokeMesh={strokeMeshTiming?.Milliseconds ?? 0:0.###}ms.");
    }

    WriteLog("GPU mesh parity verification passed.");
}

static UiEngineSession CreateGpuMeshParitySession(string path, double animationCacheWarmupSeconds)
{
    var session = new UiEngineSession(StfuRuntimeBootstrap.CreateEngine());
    if (!session.Engine.Registry.TryGet<IGpuRenderBackend>(out var gpu) || !gpu.IsAvailable)
    {
        throw new InvalidOperationException("GPU backend is not available.");
    }

    session.Workspace.Assets.SelectAssetCandidate(path, "GpuMeshParity");
    session.Workspace.Assets.LoadAssetCommand.Execute(null);
    WarmAnimationCache(session, animationCacheWarmupSeconds);
    return session;
}

static NprRenderResult RenderGpuMeshParityFrame(
    UiEngineSession session,
    INprRenderer renderer,
    int width,
    int height,
    int frame,
    NprQualityProfile quality,
    NprFrameBudget budget)
{
    var presetState = session.ActivePreset;
    var request = new NprRenderRequest(
        Revision: frame,
        Width: width,
        Height: height,
        ExecutionProfile: NprExecutionProfile.CpuDrivenGpuAccelerated,
        ContentKind: NprRenderContentKind.MeshWireframe,
        Scene: session.Engine.Scene,
        Assets: session.Assets,
        Camera: session.CameraRig.Camera,
        Settings: presetState.ActiveSettings,
        Style: presetState.ActiveGrammar,
        StyleSet: presetState.ActiveStyleSet,
        EntityStyles: session.EntityStyles,
        Analysis: session.Analysis,
        FrameHistoryState: session.FrameHistory,
        Pipeline: null,
        ActivePresetId: presetState.ActivePreset.Metadata.Id,
        ActivePipelineId: presetState.ActivePreset.PipelineId,
        FrameId: frame,
        TimeSeconds: frame / 60f,
        PreviousFrame: null,
        Quality: quality,
        Budget: budget,
        Theme: NprRenderTheme.Light,
        ShowGrid: true,
        IncludeDebugFrame: false,
        DebugOverlay: DebugOverlayKind.None,
        DiagnosticsOptions: CreateParityDiagnosticsOptions(),
        OptimizerMode: NprRenderOptimizerMode.On);

    return renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
}

static string FormatPixelAt(PixelSurface surface, int? x, int? y)
{
    if (x is null || y is null ||
        x.Value < 0 || y.Value < 0 ||
        x.Value >= surface.Width || y.Value >= surface.Height)
    {
        return "<none>";
    }

    var offset = y.Value * surface.Stride + x.Value * 4;
    return $"B{surface.Pixels[offset]}/G{surface.Pixels[offset + 1]}/R{surface.Pixels[offset + 2]}/A{surface.Pixels[offset + 3]}";
}

static ParityScenario ResolveParityScenario(string mode)
{
    return mode.ToLowerInvariant() switch
    {
        "mesh" => new ParityScenario("mesh", null, NprRenderContentKind.MeshWireframe, null, DebugOverlayKind.None),
        "default" or "npr-default" => new ParityScenario("npr-default", "default", NprRenderContentKind.NprPipeline, NprPipelineIds.Default, DebugOverlayKind.None),
        "comic" or "npr-comic" or "comic-surface" => new ParityScenario("npr-comic", "comic-surface", NprRenderContentKind.NprPipeline, NprPipelineIds.ComicSurface, DebugOverlayKind.None),
        "debug-feature-curves" => new ParityScenario("debug-feature-curves", "default", NprRenderContentKind.NprPipeline, NprPipelineIds.Default, DebugOverlayKind.FeatureCurves),
        _ => throw new InvalidOperationException($"Unsupported parity mode '{mode}'.")
    };
}

static ParityRunner CreateParityRunner(int width, int height, ParityScenario scenario, NprRenderOptimizerMode optimizerMode)
{
    var engine = StfuRuntimeBootstrap.CreateEngine();
    var renderer = engine.Registry.GetRequired<INprRenderer>();
    var assets = engine.Registry.GetRequired<AssetRegistry>();
    var camera = engine.Registry.GetRequired<CameraRig>();
    var presetState = engine.Registry.GetRequired<ActiveNprPresetState>();
    var entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
    var analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
    var frameHistory = engine.Registry.GetRequired<FrameHistoryState>();

    if (!string.IsNullOrWhiteSpace(scenario.PresetId))
    {
        presetState.ApplyPreset(scenario.PresetId);
        frameHistory.Reset();
    }

    return new ParityRunner(
        engine,
        renderer,
        assets,
        camera,
        presetState,
        entityStyles,
        analysis,
        frameHistory,
        width,
        height,
        scenario,
        CreateParityDiagnosticsOptions(),
        optimizerMode);
}

static NprRenderParitySnapshot CreateParitySnapshot(ParityScenario scenario, NprRenderResult result, int frame)
{
    var surface = result.PixelSurfaceLease?.Surface
        ?? throw new InvalidOperationException("Parity render did not produce PixelSurface output.");

    return new NprRenderParitySnapshot(
        Revision: result.Revision,
        Width: surface.Width,
        Height: surface.Height,
        ExecutionProfile: result.ExecutionProfile,
        ContentKind: scenario.ContentKind,
        ActivePresetId: scenario.PresetId ?? "<none>",
        ActivePipelineId: scenario.ExpectedPipelineId ?? string.Empty,
        StrokeFrameHash: NprRenderParityHasher.HashStrokeFrame(result.StrokeFrame),
        NprFrameHash: NprRenderParityHasher.HashNprFrame(result.NprFrame),
        DebugFrameHash: NprRenderParityHasher.HashDebugFrame(result.DebugFrame),
        PixelHash: NprRenderParityHasher.HashPixelSurface(surface),
        PathCount: result.StrokeFrame.Paths.Count,
        LayerCount: result.NprFrame.Layers.Count,
        ToneSurfaceCount: result.Diagnostics.ToneSurfaceCount);
}

static NprDiagnosticsOptions CreateSmokeDiagnosticsOptions(bool enableRangeTimings = false)
{
    return NprDiagnosticsOptions.Smoke with
    {
        EnableRangeTimings = enableRangeTimings,
        EnableDetailedStepNotes = enableRangeTimings || NprDiagnosticsOptions.Smoke.EnableDetailedStepNotes
    };
}

static NprDiagnosticsOptions CreateBenchmarkDiagnosticsOptions()
{
    return NprDiagnosticsOptions.Benchmark;
}

static NprDiagnosticsOptions CreateParityDiagnosticsOptions()
{
    return NprDiagnosticsOptions.Parity;
}

static (int Index, Vector3 Position)[] CapturePositionSamples(MeshData mesh)
{
    var count = mesh.Vertices.Count;
    if (count == 0)
    {
        return [];
    }

    var stride = NumericMath.AtLeast(count / 256, 1);
    var samples = new List<(int Index, Vector3 Position)>(NumericMath.AtMost(256, count));
    for (var i = 0; i < count; i += stride)
    {
        samples.Add((i, mesh.Vertices[i].Position));
    }

    return samples.ToArray();
}

static float CalculateSampleDelta(IReadOnlyList<(int Index, Vector3 Position)> first, MeshData second)
{
    if (first.Count == 0 || second.Vertices.Count == 0)
    {
        return 0;
    }

    var total = 0f;
    var samples = 0;
    foreach (var sample in first)
    {
        if ((uint)sample.Index >= (uint)second.Vertices.Count)
        {
            continue;
        }

        total += Vector3.Distance(sample.Position, second.Vertices[sample.Index].Position);
        samples++;
    }

    return samples == 0 ? 0 : total / samples;
}

internal sealed record ParityScenario(
    string Label,
    string? PresetId,
    NprRenderContentKind ContentKind,
    string? ExpectedPipelineId,
    DebugOverlayKind DebugOverlay);

internal sealed class ParityRunner
{
    private readonly StfuEngine _engine;
    private readonly INprRenderer _renderer;
    private readonly AssetRegistry _assets;
    private readonly CameraRig _camera;
    private readonly ActiveNprPresetState _presetState;
    private readonly NprEntityStyleRegistry _entityStyles;
    private readonly MeshAnalysisCacheStore _analysis;
    private readonly FrameHistoryState _frameHistory;
    private readonly int _width;
    private readonly int _height;
    private readonly ParityScenario _scenario;
    private readonly NprDiagnosticsOptions _diagnosticsOptions;
    private readonly NprRenderOptimizerMode _optimizerMode;

    public ParityRunner(
        StfuEngine engine,
        INprRenderer renderer,
        AssetRegistry assets,
        CameraRig camera,
        ActiveNprPresetState presetState,
        NprEntityStyleRegistry entityStyles,
        MeshAnalysisCacheStore analysis,
        FrameHistoryState frameHistory,
        int width,
        int height,
        ParityScenario scenario,
        NprDiagnosticsOptions diagnosticsOptions,
        NprRenderOptimizerMode optimizerMode)
    {
        _engine = engine;
        _renderer = renderer;
        _assets = assets;
        _camera = camera;
        _presetState = presetState;
        _entityStyles = entityStyles;
        _analysis = analysis;
        _frameHistory = frameHistory;
        _width = width;
        _height = height;
        _scenario = scenario;
        _diagnosticsOptions = diagnosticsOptions;
        _optimizerMode = optimizerMode;
    }

    public NprRenderResult Render(int frame)
    {
        var request = new NprRenderRequest(
            Revision: frame,
            Width: _width,
            Height: _height,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
            ContentKind: _scenario.ContentKind,
            Scene: _engine.Scene,
            Assets: _assets,
            Camera: _camera.Camera,
            Settings: _presetState.ActiveSettings,
            Style: _presetState.ActiveGrammar,
            StyleSet: _presetState.ActiveStyleSet,
            EntityStyles: _entityStyles,
            Analysis: _analysis,
            FrameHistoryState: _frameHistory,
            Pipeline: _scenario.ContentKind == NprRenderContentKind.NprPipeline ? _presetState.ActivePipeline : null,
            ActivePresetId: _presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: _presetState.ActivePreset.PipelineId,
            FrameId: _frameHistory.PeekNextFrameId(),
            TimeSeconds: frame / 60f,
            PreviousFrame: _frameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(),
            Theme: NprRenderTheme.Light,
            ShowGrid: _scenario.ContentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: _scenario.DebugOverlay != DebugOverlayKind.None,
            DebugOverlay: _scenario.DebugOverlay,
            DiagnosticsOptions: _diagnosticsOptions,
            OptimizerMode: _optimizerMode);

        if (_scenario.ExpectedPipelineId is not null &&
            !string.Equals(request.ActivePipelineId, _scenario.ExpectedPipelineId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Parity runner expected pipeline '{_scenario.ExpectedPipelineId}' but used '{request.ActivePipelineId}'.");
        }

        return _renderer.RenderAsync(request, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }
}

enum BenchmarkAnimationMode
{
    WallClock,
    FixedStep,
    Off
}

internal sealed class Win32SmokeWindow : IDisposable
{
    public Win32SmokeWindow(int width, int height)
    {
        Handle = CreateWindowExW(
            0,
            "STATIC",
            "STFU GPU Smoke",
            0x10000000,
            0,
            0,
            NumericMath.AtLeast(width, 1),
            NumericMath.AtLeast(height, 1),
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"CreateWindowExW failed with {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}.");
        }
    }

    public IntPtr Handle { get; }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            DestroyWindow(Handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        int exStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parentHandle,
        IntPtr menuHandle,
        IntPtr instanceHandle,
        IntPtr param);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hwnd);
}
