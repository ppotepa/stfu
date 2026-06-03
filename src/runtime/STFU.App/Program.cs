using STFU.Import.Fbx;

using STFU.UI;

WriteLog("Starting STFU host.");

try
{
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
