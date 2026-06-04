using System.Diagnostics;
using System.Numerics;
using STFU.Assets;
using STFU.Camera;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.NPR.Pipeline;
using STFU.NPR.Temporal;
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

static void CompareDefaultSnapshots(string[] args)
{
    if (args.Length < 3)
    {
        throw new InvalidOperationException(
            "Usage: --compare-default-snapshots <left.json> <right.json>");
    }

    var leftPath = Path.GetFullPath(args[1]);
    var rightPath = Path.GetFullPath(args[2]);
    var options = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    var left = System.Text.Json.JsonSerializer.Deserialize<DefaultParitySnapshot>(
        File.ReadAllText(leftPath),
        options)
        ?? throw new InvalidOperationException($"Could not deserialize snapshot: {leftPath}");

    var right = System.Text.Json.JsonSerializer.Deserialize<DefaultParitySnapshot>(
        File.ReadAllText(rightPath),
        options)
        ?? throw new InvalidOperationException($"Could not deserialize snapshot: {rightPath}");

    var comparison = DefaultParitySnapshotComparer.Compare(left, right);
    WriteLog(comparison.ToConsoleReport());
}

static int TryParsePositiveInt(string[] args, int index, int fallback)
{
    return args.Length > index && int.TryParse(args[index], out var value) && value > 0
        ? value
        : fallback;
}

static void SmokeFullCpu(string[] args)
{
    var width = TryParsePositiveInt(args, 1, 800);
    var height = TryParsePositiveInt(args, 2, 600);

    WriteLog($"Running Full CPU smoke test at {width}x{height}.");

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
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe);

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

    WriteLog($"Running GPU readback smoke test at {width}x{height}.");

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
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(RequireGpuReadback: true, PreferGpuPresentation: false),
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: debugOverlay != DebugOverlayKind.None,
            DebugOverlay: debugOverlay);

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

            if (Math.Abs(r - paper.R) > tolerance ||
                Math.Abs(g - paper.G) > tolerance ||
                Math.Abs(b - paper.B) > tolerance ||
                Math.Abs(a - 255) > tolerance)
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

    WriteLog($"Running GPU direct-present smoke test at {width}x{height}.");

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
            DebugOverlay: debugOverlay);

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
            $"Bench '{label}': CPU total={cpu.totalMs:0.00}ms ({1000.0 / Math.Max(0.001, cpu.totalMs):0.0} FPS), " +
            $"GPU total={gpuResult.totalMs:0.00}ms ({1000.0 / Math.Max(0.001, gpuResult.totalMs):0.0} FPS), " +
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
                    DebugOverlay: DebugOverlayKind.None);

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

            var divisor = Math.Max(1, frames);
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

static void BenchFbxMeshRender(string[] args)
{
    var path = args.Length > 1 ? args[1] : Path.Combine("assets", "walking.fbx");
    var width = TryParsePositiveInt(args, 2, 800);
    var height = TryParsePositiveInt(args, 3, 600);
    var frameCount = TryParsePositiveInt(args, 4, 12);
    var tileSize = TryParsePositiveInt(args, 5, 32);
    var workerCount = args.Length > 6 && int.TryParse(args[6], out var workers) ? workers : 0;
    var mode = args.Length > 7 && !string.IsNullOrWhiteSpace(args[7])
        ? args[7]
        : "mesh";
    var contentKind = string.Equals(mode, "mesh", StringComparison.OrdinalIgnoreCase)
        ? NprRenderContentKind.MeshWireframe
        : NprRenderContentKind.NprPipeline;
    var fullPath = Path.GetFullPath(path);

    WriteLog($"Benchmarking FBX {mode} render: {fullPath}, size={width}x{height}, frames={frameCount}, tile={tileSize}, workers={workerCount}.");

    var session = new UiEngineSession(StfuRuntimeBootstrap.CreateEngine());
    session.Workspace.Assets.SelectAssetCandidate(fullPath, "Bench");
    session.Workspace.Assets.LoadAssetCommand.Execute(null);

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
    var paths = 0;
    var stepTotals = new Dictionary<string, double>(StringComparer.Ordinal);
    var stepAllocTotals = new Dictionary<string, long>(StringComparer.Ordinal);

    for (var frame = 1; frame <= frameCount; frame++)
    {
        var totalWatch = Stopwatch.StartNew();

        var animationWatch = Stopwatch.StartNew();
        session.Workspace.Assets.TickAnimation();
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
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(MaxWorkerThreads: workerCount, TileSize: tileSize),
            Theme: NprRenderTheme.Light,
            ShowGrid: contentKind == NprRenderContentKind.MeshWireframe,
            IncludeDebugFrame: false);

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

    var divisor = Math.Max(1, frameCount);
    WriteLog(
        $"FBX {mode} render benchmark: avgTotal={totalMs / divisor:0.00}ms ({1000.0 / Math.Max(0.001, totalMs / divisor):0.0} FPS), " +
        $"avgAnimation={animationMs / divisor:0.00}ms, avgRender={renderMs / divisor:0.00}ms, " +
        $"avgPipeline={pipelineMs / divisor:0.00}ms, avgMesh={meshMs / divisor:0.00}ms, " +
        $"avgRaster={rasterMs / divisor:0.00}ms, avgAlloc={allocatedBytes / divisor / (1024.0 * 1024.0):0.00}MB, paths={paths}.");

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

static (int Index, Vector3 Position)[] CapturePositionSamples(MeshData mesh)
{
    var count = mesh.Vertices.Count;
    if (count == 0)
    {
        return [];
    }

    var stride = Math.Max(1, count / 256);
    var samples = new List<(int Index, Vector3 Position)>(Math.Min(256, count));
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
            Math.Max(1, width),
            Math.Max(1, height),
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
