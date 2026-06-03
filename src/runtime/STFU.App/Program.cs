using System.Numerics;
using STFU.Assets;
using STFU.Common.Primitives;
using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Mesh;
using STFU.Mesh.Commands;
using STFU.Mesh.Loading;
using STFU.MeshIO;
using STFU.Messaging.Commands;
using STFU.Projection;
using STFU.Projection.Commands;
using STFU.Strokes;
using STFU.Viewport;
using STFU.Viewport.Commands;

var engine = StfuEngineBuilder
    .Create()
    .AddModule(new AssetsModule())
    .AddModule(new MeshModule())
    .AddModule(new MeshIOModule())
    .AddModule(new ProjectionModule())
    .AddModule(new StrokesModule())
    .AddModule(new ViewportModule())
    .Build();
var meshFactory = engine.Registry.GetRequired<MeshFactory>();
var meshLoader = engine.Registry.GetRequired<IMeshLoader<string>>();
var commands = new CommandBuffer();

commands.Enqueue(new CreateEntityCommand("Suzanne"));
var handled = engine.Tick(commands);

var entity = engine.Scene.Entities[0];
commands.Enqueue(new AssignMeshToEntityCommand(entity.Id, new MeshHandle(1)));
commands.Enqueue(new SetEntityPositionCommand(entity.Id, new Vector3(0, 1, 0)));
commands.Enqueue(new SetCameraCommand(CameraState.Default));
commands.Enqueue(new SetViewportSizeCommand(1280, 720));
commands.Enqueue(new RequestRenderCommand());
handled += engine.Tick(commands);

var meshResult = TryLoadMesh(meshFactory, meshLoader);
var assets = engine.Registry.GetRequired<AssetRegistry>();
var projection = engine.Registry.GetRequired<ProjectionState>();
var viewport = engine.Registry.GetRequired<ViewportState>();

Console.WriteLine("STFU boilerplate");
Console.WriteLine($"Registered services: {engine.Registry.Count}");
Console.WriteLine($"Registered command handlers: {engine.Commands.HandlerCount}");
Console.WriteLine($"Handled commands: {handled}");
Console.WriteLine($"Entities: {engine.Scene.Entities.Count}");
Console.WriteLine($"Entity: {entity.Name} mesh={entity.Mesh.Value} position={entity.Position}");
Console.WriteLine($"Assets meshes: {assets.MeshCount}");
Console.WriteLine($"Camera: {projection.Camera.Position} -> {projection.Camera.Target}");
Console.WriteLine($"Viewport: {viewport.Snapshot.Width}x{viewport.Snapshot.Height}");
Console.WriteLine($"Mesh loader: {meshResult}");

static string TryLoadMesh(MeshFactory meshFactory, IMeshLoader<string> meshLoader)
{
    try
    {
        _ = meshFactory.Load("assets/suzanne.obj", meshLoader);
        return "loaded";
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message;
    }
}
