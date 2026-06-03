using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using STFU.Assets;
using STFU.Camera;
using STFU.Camera.Commands;
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
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
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

        var viewport = new EngineViewportControl(CreateEngine());
        Content = viewport;
        KeyDown += viewport.HandleKeyDown;
        Opened += (_, _) =>
        {
            StfuUiLog.Write("Main window opened.");
            viewport.Focus();
        };
        Closed += (_, _) => StfuUiLog.Write("Main window closed.");
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
            .AddModule(new ViewportModule())
            .AddModule(new NprModule())
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
        StfuUiLog.Write($"Loaded mesh asset: {path}");
        StfuUiLog.Write($"Mesh vertices: {mesh.Vertices.Count}, triangles: {mesh.Triangles.Count}");

        return assets.AddMesh(path, mesh);
    }
}

internal sealed class EngineViewportControl : Control
{
    private readonly StfuEngine _engine;
    private readonly AssetRegistry _assets;
    private readonly CameraRig _camera;
    private readonly INprPipeline _nprPipeline;
    private readonly NprPresetRegistry _nprPresetRegistry;
    private readonly NprSettings _nprSettings;
    private readonly StrokeState _strokes;
    private readonly ViewportState _viewport;
    private readonly CommandBuffer _commands = new();
    private Point _lastPointerPosition;
    private bool _isOrbiting;
    private bool _loggedFirstFrame;
    private bool _loggedOrbitInput;
    private bool _loggedPanInput;
    private bool _loggedFovInput;

    public EngineViewportControl(StfuEngine engine)
    {
        _engine = engine;
        _assets = engine.Registry.GetRequired<AssetRegistry>();
        _camera = engine.Registry.GetRequired<CameraRig>();
        _nprPipeline = engine.Registry.GetRequired<INprPipeline>();
        _nprPresetRegistry = engine.Registry.GetRequired<NprPresetRegistry>();
        _nprSettings = engine.Registry.GetRequired<NprSettings>();
        _strokes = engine.Registry.GetRequired<StrokeState>();
        _viewport = engine.Registry.GetRequired<ViewportState>();

        Focusable = true;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var width = Math.Max(1, (int)bounds.Width);
        var height = Math.Max(1, (int)bounds.Height);

        _strokes.Publish(CreateFrame(width, height, _viewport.RenderMode));
        _commands.Enqueue(new SetViewportSizeCommand(width, height));
        _commands.Enqueue(new RequestRenderCommand());
        _engine.Tick(_commands);

        if (!_loggedFirstFrame)
        {
            StfuUiLog.Write($"Viewport first frame: {width}x{height}, strokes: {_viewport.Snapshot.Frame.Paths.Count}");
            _loggedFirstFrame = true;
        }

        context.FillRectangle(new SolidColorBrush(Color.FromRgb(245, 245, 242)), bounds);
        DrawGrid(context, bounds);
        DrawFrame(context, _viewport.Snapshot.Frame);
    }

    private static void DrawGrid(DrawingContext context, Rect bounds)
    {
        var majorPen = new Pen(new SolidColorBrush(Color.FromRgb(215, 215, 210)), 1.0);
        var minorPen = new Pen(new SolidColorBrush(Color.FromRgb(232, 232, 228)), 1.0);

        for (var x = 0.0; x <= bounds.Width; x += 24.0)
        {
            var pen = Math.Abs(x % 96.0) < 0.01 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(x, 0), new Point(x, bounds.Height));
        }

        for (var y = 0.0; y <= bounds.Height; y += 24.0)
        {
            var pen = Math.Abs(y % 96.0) < 0.01 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(0, y), new Point(bounds.Width, y));
        }
    }

    private static void DrawFrame(DrawingContext context, StrokeFrame frame)
    {
        foreach (var path in frame.Paths)
        {
            if (path.Points.Count < 2)
            {
                continue;
            }

            var color = path.Style.Color;
            var alpha = (byte)(Math.Clamp(path.Style.Opacity, 0f, 1f) * 255f);
            var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            var pen = new Pen(brush, Math.Max(0.35, path.Style.Thickness));

            for (var index = 1; index < path.Points.Count; index++)
            {
                var start = path.Points[index - 1];
                var end = path.Points[index];
                context.DrawLine(
                    pen,
                    new Point(start.X, start.Y),
                    new Point(end.X, end.Y));
            }
        }
    }

    private StrokeFrame CreateFrame(int width, int height, ViewportRenderMode renderMode)
    {
        return renderMode switch
        {
            ViewportRenderMode.Npr => CreateNprFrame(width, height),
            _ => CreateSuzanneFrame(width, height)
        };
    }

    private StrokeFrame CreateNprFrame(int width, int height)
    {
        var context = new NprContext
        {
            Scene = _engine.Scene,
            Assets = _assets,
            Camera = _camera.Camera,
            Width = width,
            Height = height,
            Settings = _nprSettings
        };

        return _nprPipeline.Execute(context);
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

        return MeshToStrokeFrame(mesh, width, height, _camera.Camera);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (!IsLeftButtonPressed(point))
        {
            return;
        }

        Focus();
        _isOrbiting = true;
        _lastPointerPosition = point.Position;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isOrbiting)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            StopOrbit(e.Pointer);
            return;
        }

        var position = point.Position;
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            const float panUnitsPerPixel = 0.005f;
            _commands.Enqueue(new PanCameraCommand((float)-delta.X * panUnitsPerPixel, (float)delta.Y * panUnitsPerPixel));

            if (!_loggedPanInput)
            {
                StfuUiLog.Write("Viewport pan input active: Ctrl + left mouse drag.");
                _loggedPanInput = true;
            }
        }
        else
        {
            const float radiansPerPixel = 0.01f;
            _commands.Enqueue(new OrbitCameraCommand((float)delta.X * radiansPerPixel, (float)-delta.Y * radiansPerPixel));

            if (!_loggedOrbitInput)
            {
                StfuUiLog.Write("Viewport orbit input active: left mouse drag.");
                _loggedOrbitInput = true;
            }
        }

        _engine.Tick(_commands);

        InvalidateVisual();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        StopOrbit(e.Pointer);
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isOrbiting = false;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        const float degreesPerWheelStep = 3f;
        _commands.Enqueue(new AdjustCameraFovCommand((float)-e.Delta.Y * degreesPerWheelStep));
        _engine.Tick(_commands);

        if (!_loggedFovInput)
        {
            StfuUiLog.Write("Viewport FOV input active: mouse wheel.");
            _loggedFovInput = true;
        }

        InvalidateVisual();
        e.Handled = true;
    }

    public void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        var renderMode = e.Key switch
        {
            Key.D1 or Key.NumPad1 => ViewportRenderMode.Mesh,
            Key.D2 or Key.NumPad2 => ViewportRenderMode.Npr,
            _ => (ViewportRenderMode?)null
        };

        if (renderMode is null)
        {
            return;
        }

        _commands.Enqueue(new SetViewportRenderModeCommand(renderMode.Value));
        _engine.Tick(_commands);
        StfuUiLog.Write($"Viewport render mode: {renderMode.Value}");
        if (renderMode.Value == ViewportRenderMode.Npr)
        {
            var preset = _nprPresetRegistry.ActivePreset.Metadata;
            StfuUiLog.Write($"NPR preset: {preset.Id} ({preset.Name})");
        }

        InvalidateVisual();
        e.Handled = true;
    }

    private void StopOrbit(IPointer pointer)
    {
        if (!_isOrbiting)
        {
            return;
        }

        _isOrbiting = false;
        pointer.Capture(null);
    }

    private static bool IsLeftButtonPressed(PointerPoint point)
    {
        return point.Properties.IsLeftButtonPressed ||
            point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
    }

    private static StrokeFrame MeshToStrokeFrame(MeshData mesh, int width, int height, CameraState camera)
    {
        if (mesh.Vertices.Count == 0)
        {
            return StrokeFrame.Empty;
        }

        var paths = new List<StrokePath2D>(mesh.Triangles.Count * 3);
        var projection = CameraProjection.Create(camera, width, height);
        var style = new StrokeStyle2D(0.55f, 1.0f, StrokeColor.Black);

        foreach (var triangle in mesh.Triangles)
        {
            AddEdge(triangle.A, triangle.B);
            AddEdge(triangle.B, triangle.C);
            AddEdge(triangle.C, triangle.A);
        }

        return new StrokeFrame(width, height, paths);

        void AddEdge(int a, int b)
        {
            if (projection.TryProject(mesh.Vertices[a].Position, out var start) &&
                projection.TryProject(mesh.Vertices[b].Position, out var end))
            {
                paths.Add(StrokePath2D.Line(start, end, style));
            }
        }
    }

    private readonly record struct CameraProjection(
        Vector3 Position,
        Vector3 Forward,
        Vector3 Right,
        Vector3 Up,
        float FocalScale,
        float Aspect,
        int Width,
        int Height)
    {
        public static CameraProjection Create(CameraState camera, int width, int height)
        {
            var forward = Vector3.Normalize(camera.Target - camera.Position);
            var right = Vector3.Cross(forward, Vector3.UnitY);

            if (right.LengthSquared() < 0.0001f)
            {
                right = Vector3.UnitX;
            }
            else
            {
                right = Vector3.Normalize(right);
            }

            var up = Vector3.Normalize(Vector3.Cross(right, forward));
            var fovRadians = MathF.PI / 180f * Math.Clamp(camera.FieldOfViewDegrees, 1f, 179f);
            var focalScale = 1f / MathF.Tan(fovRadians * 0.5f);
            var aspect = width / (float)Math.Max(1, height);

            return new CameraProjection(camera.Position, forward, right, up, focalScale, aspect, width, height);
        }

        public bool TryProject(Vector3 worldPosition, out Point2D point)
        {
            var cameraSpace = worldPosition - Position;
            var z = Vector3.Dot(cameraSpace, Forward);

            if (z <= 0.01f)
            {
                point = default;
                return false;
            }

            var x = Vector3.Dot(cameraSpace, Right);
            var y = Vector3.Dot(cameraSpace, Up);
            var normalizedX = x / z * FocalScale / Aspect;
            var normalizedY = y / z * FocalScale;

            point = new Point2D(
                (normalizedX * 0.5f + 0.5f) * Width,
                (-normalizedY * 0.5f + 0.5f) * Height);

            return true;
        }
    }
}
