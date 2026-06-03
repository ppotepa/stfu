using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using STFU.Assets;
using STFU.Common.Primitives;
using STFU.Engine;
using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Mesh;
using STFU.Mesh.Commands;
using STFU.Mesh.Loading;
using STFU.MeshIO;
using STFU.Messaging.Commands;
using STFU.Projection;
using STFU.Strokes;
using STFU.Viewport;
using STFU.Viewport.Commands;

namespace STFU.UI;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "STFU";
        Width = 1280;
        Height = 720;
        MinWidth = 640;
        MinHeight = 360;

        Content = new EngineViewportControl(CreateEngine());
    }

    private static StfuEngine CreateEngine()
    {
        var engine = StfuEngineBuilder
            .Create()
            .AddModule(new AssetsModule())
            .AddModule(new MeshModule())
            .AddModule(new MeshIOModule())
            .AddModule(new ProjectionModule())
            .AddModule(new StrokesModule())
            .AddModule(new ViewportModule())
            .Build();

        var commands = new CommandBuffer();
        commands.Enqueue(new CreateEntityCommand("Suzanne"));
        engine.Tick(commands);

        var entity = engine.Scene.Entities[0];
        var meshHandle = LoadSuzanneMesh(engine);

        commands.Enqueue(new AssignMeshToEntityCommand(entity.Id, meshHandle));
        commands.Enqueue(new SetEntityPositionCommand(entity.Id, new Vector3(0, 0, 0)));
        engine.Tick(commands);

        return engine;
    }

    private static MeshHandle LoadSuzanneMesh(StfuEngine engine)
    {
        var meshFactory = engine.Registry.GetRequired<MeshFactory>();
        var meshLoader = engine.Registry.GetRequired<IMeshLoader<string>>();
        var assets = engine.Registry.GetRequired<AssetRegistry>();
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../assets/suzanne.obj"));
        var mesh = meshFactory.Load(path, meshLoader);

        return assets.AddMesh(path, mesh);
    }
}

internal sealed class EngineViewportControl : Control
{
    private readonly StfuEngine _engine;
    private readonly AssetRegistry _assets;
    private readonly StrokeState _strokes;
    private readonly ViewportState _viewport;
    private readonly CommandBuffer _commands = new();

    public EngineViewportControl(StfuEngine engine)
    {
        _engine = engine;
        _assets = engine.Registry.GetRequired<AssetRegistry>();
        _strokes = engine.Registry.GetRequired<StrokeState>();
        _viewport = engine.Registry.GetRequired<ViewportState>();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var width = Math.Max(1, (int)bounds.Width);
        var height = Math.Max(1, (int)bounds.Height);

        _strokes.Publish(CreateSuzanneFrame(width, height));
        _commands.Enqueue(new SetViewportSizeCommand(width, height));
        _commands.Enqueue(new RequestRenderCommand());
        _engine.Tick(_commands);

        context.FillRectangle(new SolidColorBrush(Color.FromRgb(245, 245, 242)), bounds);
        DrawFrame(context, _viewport.Snapshot.Frame);
    }

    private static void DrawFrame(DrawingContext context, StrokeFrame frame)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(20, 20, 20)), 2.0);

        foreach (var stroke in frame.Strokes)
        {
            context.DrawLine(
                pen,
                new Point(stroke.Start.X, stroke.Start.Y),
                new Point(stroke.End.X, stroke.End.Y));
        }
    }

    private StrokeFrame CreateSuzanneFrame(int width, int height)
    {
        if (_engine.Scene.Entities.Count == 0)
        {
            return StrokeFrame.Empty;
        }

        var entity = _engine.Scene.Entities[0];
        if (!_assets.TryGetMesh(entity.Mesh, out var mesh))
        {
            return StrokeFrame.Empty;
        }

        return MeshToStrokeFrame(mesh, width, height);
    }

    private static StrokeFrame MeshToStrokeFrame(MeshData mesh, int width, int height)
    {
        if (mesh.Vertices.Count == 0)
        {
            return StrokeFrame.Empty;
        }

        var min = mesh.Vertices[0].Position;
        var max = mesh.Vertices[0].Position;

        foreach (var vertex in mesh.Vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        var size = max - min;
        var maxAxis = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        var scale = MathF.Min(width, height) * 0.78f / MathF.Max(0.0001f, maxAxis);
        var center = (min + max) * 0.5f;
        var screenCenter = new Vector2(width * 0.5f, height * 0.52f);
        var strokes = new List<Stroke2D>(mesh.Triangles.Count * 3);

        foreach (var triangle in mesh.Triangles)
        {
            AddEdge(triangle.A, triangle.B);
            AddEdge(triangle.B, triangle.C);
            AddEdge(triangle.C, triangle.A);
        }

        return new StrokeFrame(width, height, strokes);

        void AddEdge(int a, int b)
        {
            var start = Project(mesh.Vertices[a].Position, center, scale, screenCenter);
            var end = Project(mesh.Vertices[b].Position, center, scale, screenCenter);
            strokes.Add(new Stroke2D(start, end, 0.55f));
        }
    }

    private static Point2D Project(
        Vector3 position,
        Vector3 center,
        float scale,
        Vector2 screenCenter)
    {
        var p = position - center;
        var x = (p.X - p.Z * 0.28f) * scale + screenCenter.X;
        var y = (-p.Y + p.Z * 0.16f) * scale + screenCenter.Y;

        return new Point2D(x, y);
    }
}
