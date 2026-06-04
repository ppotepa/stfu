using STFU.Assets;
using STFU.Camera;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.NPR.Pipeline;
using STFU.NPR.Temporal;
using STFU.Import.Fbx;
using STFU.UI;

WriteLog("Starting STFU host.");

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

    StfuUiHost.Run(args, WriteLog);
    WriteLog("STFU UI stopped.");
}
catch (Exception exception)
{
    WriteLog($"Fatal error: {exception}");
    Environment.ExitCode = 1;
}

static void WriteLog(string message)
{
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
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
