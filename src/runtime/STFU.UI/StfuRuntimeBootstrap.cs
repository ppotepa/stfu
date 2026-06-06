using System.Numerics;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Engine;
using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Mesh;
using STFU.Mesh.Commands;
using STFU.Mesh.Loading;
using STFU.MeshIO;
using STFU.Messaging.Commands;
using STFU.NPR.Composition;
using STFU.NPR.Pipelines;
using STFU.NPR.Presets;
using STFU.Rendering.Cpu;
using STFU.Rendering.DirectX;
using STFU.Strokes;
using STFU.Viewport;

namespace STFU.UI;

public static class StfuRuntimeBootstrap
{
    public static StfuEngine CreateEngine()
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
            .AddModule(new ViewportModule())
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
            entity.Transform = CreateHtmlParityTransform(mesh);
        }

        return engine;
    }

    public static MeshHandle LoadSuzanneMesh(StfuEngine engine)
    {
        var meshFactory = engine.Registry.GetRequired<MeshFactory>();
        var meshLoader = engine.Registry.GetRequired<IMeshLoader<string>>();
        var assets = engine.Registry.GetRequired<AssetRegistry>();
        var path = ResolveAssetPath("suzanne.obj");
        var mesh = meshFactory.Load(path, meshLoader);
        StfuUiLog.Write($"Loaded mesh asset: {path}");
        StfuUiLog.Write($"Mesh vertices: {mesh.Vertices.Count}, triangles: {mesh.Triangles.Count}");

        return assets.AddMesh(path, mesh);
    }

    public static string ResolveAssetPath(string fileName)
    {
        foreach (var root in EnumerateAssetRoots())
        {
            var path = Path.GetFullPath(Path.Combine(root, "assets", fileName));
            if (File.Exists(path))
            {
                return path;
            }
        }

        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "assets", fileName));
    }

    public static IEnumerable<string> EnumerateAssetRoots()
    {
        yield return Environment.CurrentDirectory;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private static Transform3D CreateHtmlParityTransform(MeshData mesh)
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
