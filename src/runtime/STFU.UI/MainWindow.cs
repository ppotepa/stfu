using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Runtime.InteropServices;
using STFU.Assets;
using STFU.Camera;
using STFU.Camera.Commands;
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
using STFU.NPR.Debug;
using STFU.NPR.Pipeline;
using STFU.NPR.Rendering;
using STFU.NPR.Settings;
using STFU.NPR.Analysis;
using STFU.NPR.Preset.Blueprint;
using STFU.NPR.Preset.MangaInk;
using STFU.NPR.Preset.PencilConstruction;
using STFU.NPR.Preset.PenInkHatching;
using STFU.NPR.Preset.TechnicalInk;
using STFU.NPR.Pipeline.ComicSurface;
using STFU.NPR.Temporal;
using STFU.Strokes;
using STFU.Viewport;
using STFU.Viewport.Commands;

namespace STFU.UI;

public sealed class MainWindow : Window
{
    public MainWindow()
        : this(StfuUiStartupOptions.Default)
    {
    }

    internal MainWindow(StfuUiStartupOptions startupOptions)
    {
        Title = "STFU";
        Width = 1280;
        Height = 720;
        MinWidth = 640;
        MinHeight = 360;

        var viewport = new EngineViewportControl(CreateEngine(), startupOptions);
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
            .AddModule(new NprModule(
                [
                    new TechnicalInkPreset(),
                    new PencilConstructionPreset(),
                    new PenInkHatchingPreset(),
                    new MangaInkPreset(),
                    new BlueprintPreset(),
                    new ComicSurfacePreset()
                ],
                [
                    new ComicSurfacePipelineProvider()
                ]))
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
        var path = ResolveAssetPath("suzanne.obj");
        var mesh = meshFactory.Load(path, meshLoader);
        StfuUiLog.Write($"Loaded mesh asset: {path}");
        StfuUiLog.Write($"Mesh vertices: {mesh.Vertices.Count}, triangles: {mesh.Triangles.Count}");

        return assets.AddMesh(path, mesh);
    }

    private static string ResolveAssetPath(string fileName)
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

    private static IEnumerable<string> EnumerateAssetRoots()
    {
        yield return Environment.CurrentDirectory;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }
}

internal sealed class EngineViewportControl : Control
{
    private readonly StfuEngine _engine;
    private readonly AssetRegistry _assets;
    private readonly CameraRig _camera;
    private readonly ActiveNprPresetState _activeNprPreset;
    private readonly NprEntityStyleRegistry _entityStyles;
    private readonly NprFrameState _nprFrames;
    private readonly MeshAnalysisCacheStore _analysis;
    private readonly FrameHistoryState _frameHistory;
    private readonly NprDebugState _debug;
    private readonly StrokeState _strokes;
    private readonly ViewportState _viewport;
    private readonly CommandBuffer _commands = new();
    private Point _lastPointerPosition;
    private bool _isOrbiting;
    private bool _loggedFirstFrame;
    private bool _loggedOrbitInput;
    private bool _loggedPanInput;
    private bool _loggedFovInput;
    private bool _logNextNprFrameSummary = true;
    private readonly DispatcherTimer _renderTimer;

    public EngineViewportControl(StfuEngine engine)
        : this(engine, StfuUiStartupOptions.Default)
    {
    }

    internal EngineViewportControl(StfuEngine engine, StfuUiStartupOptions startupOptions)
    {
        _engine = engine;
        _assets = engine.Registry.GetRequired<AssetRegistry>();
        _camera = engine.Registry.GetRequired<CameraRig>();
        _activeNprPreset = engine.Registry.GetRequired<ActiveNprPresetState>();
        _entityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
        _nprFrames = engine.Registry.GetRequired<NprFrameState>();
        _analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
        _frameHistory = engine.Registry.GetRequired<FrameHistoryState>();
        _debug = engine.Registry.GetRequired<NprDebugState>();
        _strokes = engine.Registry.GetRequired<StrokeState>();
        _viewport = engine.Registry.GetRequired<ViewportState>();

        ApplyStartupOptions(startupOptions);

        Focusable = true;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
        PointerWheelChanged += OnPointerWheelChanged;

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _renderTimer.Tick += (_, _) => InvalidateVisual();

        AttachedToVisualTree += (_, _) =>
        {
            if (!_renderTimer.IsEnabled)
            {
                _renderTimer.Start();
                StfuUiLog.Write("Viewport render loop started.");
            }
        };

        DetachedFromVisualTree += (_, _) =>
        {
            if (_renderTimer.IsEnabled)
            {
                _renderTimer.Stop();
                StfuUiLog.Write("Viewport render loop stopped.");
            }
        };
    }

    private void ApplyStartupOptions(StfuUiStartupOptions startupOptions)
    {
        if (!string.IsNullOrWhiteSpace(startupOptions.PresetId))
        {
            _activeNprPreset.ApplyPreset(startupOptions.PresetId);
            _frameHistory.Reset();
            var preset = _activeNprPreset.ActivePreset.Metadata;
            StfuUiLog.Write($"Startup NPR preset: {preset.Id} ({preset.Name})");
            _logNextNprFrameSummary = true;
        }

        if (startupOptions.RenderMode is { } renderMode)
        {
            _commands.Enqueue(new SetViewportRenderModeCommand(renderMode));
            _engine.Tick(_commands);
            StfuUiLog.Write($"Startup viewport render mode: {renderMode}");
            _logNextNprFrameSummary = renderMode != ViewportRenderMode.Mesh;
        }
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
            StfuUiLog.Write($"Viewport first frame: {width}x{height}, strokes: {_viewport.Snapshot.Frame.Paths.Count}, layers: {_viewport.Snapshot.NprFrame?.Layers.Count ?? 0}");
            _loggedFirstFrame = true;
        }

        if (_viewport.Snapshot.NprFrame is { Layers.Count: > 0 } nprFrame && _viewport.RenderMode != ViewportRenderMode.Mesh)
        {
            DrawNprFrame(context, nprFrame, bounds);
        }
        else
        {
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(245, 245, 242)), bounds);
            DrawGrid(context, bounds);
            DrawFrame(context, _viewport.Snapshot.Frame);
        }

        DrawDebugOverlay(context, _viewport.Snapshot.DebugFrame, _viewport.DebugOverlay);
    }

    private static void DrawNprFrame(DrawingContext context, NprFrame frame, Rect bounds)
    {
        var paper = frame.Paper.Color;
        var paperAlpha = (byte)(Math.Clamp(frame.Paper.Opacity, 0f, 1f) * 255f);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(paperAlpha, paper.R, paper.G, paper.B)), bounds);

        foreach (var layer in frame.Layers.Where(layer => layer.Visible).OrderBy(layer => layer.Order))
        {
            var layerOpacity = Math.Clamp(layer.Opacity, 0f, 1f);
            foreach (var tone in layer.Tones)
            {
                DrawToneSurface(context, tone, bounds, layerOpacity);
            }

            DrawPaths(context, layer.Shading, layerOpacity);
            DrawPaths(context, layer.Strokes, layerOpacity);
        }
    }

    private static void DrawToneSurface(DrawingContext context, NprToneSurface2D surface, Rect bounds, float layerOpacity)
    {
        if (surface.Width <= 0 || surface.Height <= 0 || surface.Rgba.Length < surface.Width * surface.Height * 4)
        {
            return;
        }

        var bitmap = new WriteableBitmap(
            new PixelSize(surface.Width, surface.Height),
            new Avalonia.Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        using (var framebuffer = bitmap.Lock())
        {
            var pixels = new byte[framebuffer.RowBytes * surface.Height];
            var opacity = Math.Clamp(surface.Opacity * layerOpacity, 0f, 1f);
            for (var y = 0; y < surface.Height; y++)
            {
                var sourceRow = y * surface.Width * 4;
                var targetRow = y * framebuffer.RowBytes;
                for (var x = 0; x < surface.Width; x++)
                {
                    var source = sourceRow + x * 4;
                    var target = targetRow + x * 4;
                    var alpha = (byte)Math.Clamp(MathF.Round(surface.Rgba[source + 3] * opacity), 0f, 255f);
                    pixels[target] = Premultiply(surface.Rgba[source + 2], alpha);
                    pixels[target + 1] = Premultiply(surface.Rgba[source + 1], alpha);
                    pixels[target + 2] = Premultiply(surface.Rgba[source], alpha);
                    pixels[target + 3] = alpha;
                }
            }

            Marshal.Copy(pixels, 0, framebuffer.Address, pixels.Length);
        }

        context.DrawImage(
            bitmap,
            new Rect(0, 0, surface.Width, surface.Height),
            bounds);
    }

    private static byte Premultiply(byte color, byte alpha)
    {
        return (byte)(color * alpha / 255);
    }

    private static void DrawPaths(DrawingContext context, IReadOnlyList<StrokePath2D> paths, float layerOpacity)
    {
        foreach (var path in paths.OrderByDescending(path => path.Metadata?.LayerOrder ?? 100))
        {
            DrawPath(context, path, layerOpacity);
        }
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
        foreach (var path in frame.Paths.OrderByDescending(path => path.Metadata?.LayerOrder ?? 100))
        {
            DrawPath(context, path, 1f);
        }
    }

    private static void DrawPath(DrawingContext context, StrokePath2D path, float opacityScale)
    {
        if (path.Points.Count < 2)
        {
            return;
        }

        var dashStyle = path.Metadata?.SourceKind == "DashedHiddenStroke"
            ? new DashStyle([6.0, 4.0], 0)
            : null;

        if (path.RichPoints is { Count: > 1 } richPoints && richPoints.Count == path.Points.Count)
        {
            for (var index = 1; index < richPoints.Count; index++)
            {
                var start = richPoints[index - 1];
                var end = richPoints[index];
                var style = new StrokeStyle2D(
                    MathF.Max(0.35f, (start.Thickness + end.Thickness) * 0.5f),
                    Math.Clamp((start.Opacity + end.Opacity) * 0.5f * opacityScale, 0f, 1f),
                    path.Style.Color);
                context.DrawLine(
                    CreatePen(style, dashStyle),
                    new Point(start.Position.X, start.Position.Y),
                    new Point(end.Position.X, end.Position.Y));
            }

            return;
        }

        var pen = CreatePen(path.Style with { Opacity = Math.Clamp(path.Style.Opacity * opacityScale, 0f, 1f) }, dashStyle);

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

    private static Pen CreatePen(StrokeStyle2D style, DashStyle? dashStyle)
    {
        var color = style.Color;
        var alpha = (byte)(Math.Clamp(style.Opacity, 0f, 1f) * 255f);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        return new Pen(brush, Math.Max(0.35, style.Thickness), dashStyle);
    }

    private static void DrawDebugOverlay(DrawingContext context, NprDebugFrame debugFrame, DebugOverlayKind overlay)
    {
        if (overlay == DebugOverlayKind.None || debugFrame.Lines.Count == 0)
        {
            return;
        }

        foreach (var line in debugFrame.Lines)
        {
            if (line.Kind != overlay)
            {
                continue;
            }

            var color = overlay switch
            {
                DebugOverlayKind.FeatureCurves => line.Label switch
                {
                    "Boundary" => Color.FromArgb(220, 215, 110, 40),
                    "Silhouette" => Color.FromArgb(235, 35, 35, 35),
                    "Crease" => Color.FromArgb(220, 35, 110, 210),
                    "SurfaceFlow" => Color.FromArgb(220, 55, 155, 105),
                    "Hatch" => Color.FromArgb(190, 135, 70, 160),
                    _ => Color.FromArgb(210, 90, 90, 90)
                },
                DebugOverlayKind.VisibilitySegments => line.IsPrimary
                    ? Color.FromArgb(220, 30, 150, 80)
                    : Color.FromArgb(200, 190, 60, 60),
                DebugOverlayKind.SalienceHeatmap => ColorFromHeat(line.Value, (byte)(line.IsPrimary ? 235 : 155)),
                DebugOverlayKind.StrokeCandidates => line.Label switch
                {
                    "Silhouette" => Color.FromArgb(235, 25, 25, 25),
                    "Boundary" => Color.FromArgb(220, 30, 30, 30),
                    "Crease" => Color.FromArgb(220, 45, 120, 220),
                    "SurfaceFlow" => Color.FromArgb(210, 70, 170, 110),
                    "Hatch" => Color.FromArgb(200, 145, 80, 170),
                    _ => Color.FromArgb(210, 90, 90, 90)
                },
                DebugOverlayKind.ToneField => ColorFromHeat(line.Value, 210),
                DebugOverlayKind.DirectionField => Color.FromArgb(210, 50, 120, 220),
                DebugOverlayKind.DensityField => Color.FromArgb(210, 145, 85, 195),
                DebugOverlayKind.TextureField => Color.FromArgb(210, 180, 120, 55),
                DebugOverlayKind.TemporalMatches => line.IsPrimary
                    ? Color.FromArgb(220, 35, 175, 215)
                    : Color.FromArgb(220, 215, 125, 35),
                DebugOverlayKind.GhostStrokes => Color.FromArgb(180, 150, 150, 150),
                DebugOverlayKind.HatchingPlan => line.Label switch
                {
                    "Primary" => Color.FromArgb(220, 125, 70, 165),
                    "Cross" => Color.FromArgb(220, 70, 135, 195),
                    "Tertiary" => Color.FromArgb(220, 55, 110, 125),
                    _ => Color.FromArgb(210, 120, 120, 120)
                },
                DebugOverlayKind.StyleMask => Color.FromArgb(220, 230, 130, 35),
                DebugOverlayKind.MaterialRegion => Color.FromArgb(220, 55, 155, 210),
                _ => Color.FromArgb(200, 90, 90, 90)
            };

            var thickness = overlay switch
            {
                DebugOverlayKind.FeatureCurves => 1.5,
                DebugOverlayKind.SalienceHeatmap => 2.0,
                DebugOverlayKind.StrokeCandidates => 1.6,
                DebugOverlayKind.ToneField => 1.4,
                DebugOverlayKind.DirectionField => 1.25,
                DebugOverlayKind.DensityField => 1.5,
                DebugOverlayKind.TextureField => 1.35,
                DebugOverlayKind.TemporalMatches => line.IsPrimary ? 1.7 : 1.45,
                DebugOverlayKind.GhostStrokes => 1.3,
                DebugOverlayKind.HatchingPlan => line.IsPrimary ? 1.8 : 1.35,
                DebugOverlayKind.StyleMask => 2.0,
                DebugOverlayKind.MaterialRegion => 1.7,
                _ => 1.25
            };
            var pen = new Pen(new SolidColorBrush(color), thickness);
            context.DrawLine(
                pen,
                new Point(line.Start.X, line.Start.Y),
                new Point(line.End.X, line.End.Y));
        }

        static Color ColorFromHeat(float value, byte alpha)
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            var red = (byte)(220f * (1f - clamped) + 20f * clamped);
            var green = (byte)(40f + 175f * clamped);
            var blue = (byte)(45f + 35f * (1f - clamped));
            return Color.FromArgb(alpha, red, green, blue);
        }
    }

    private StrokeFrame CreateFrame(int width, int height, ViewportRenderMode renderMode)
    {
        return renderMode switch
        {
            ViewportRenderMode.Npr => CreateNprFrame(width, height),
            ViewportRenderMode.ComicSurface => CreateNprFrame(width, height),
            _ => CreateMeshFrame(width, height)
        };
    }

    private StrokeFrame CreateMeshFrame(int width, int height)
    {
        _debug.Publish(NprDebugFrame.Empty);
        _nprFrames.Publish(NprFrame.Empty);
        return CreateSuzanneFrame(width, height);
    }

    private StrokeFrame CreateNprFrame(int width, int height)
    {
        var presetState = _activeNprPreset;
        var nprContext = new NprContext
        {
            FrameId = _frameHistory.PeekNextFrameId(),
            TimeSeconds = _frameHistory.PeekNextFrameId() / 60f,
            PreviousFrame = _frameHistory.GetPreviousFrame(),
            Scene = _engine.Scene,
            Assets = _assets,
            Camera = _camera.Camera,
            Width = width,
            Height = height,
            Settings = presetState.ActiveSettings,
            Style = presetState.ActiveGrammar,
            StyleSet = presetState.ActiveStyleSet,
            EntityStyles = _entityStyles,
            Analysis = _analysis,
            FrameHistoryState = _frameHistory
        };

        var frame = presetState.ActivePipeline.Execute(nprContext);
        _nprFrames.Publish(nprContext.NprFrame);
        _debug.Publish(nprContext.DebugFrame);
        LogNprFrameSummaryIfNeeded(presetState, nprContext, frame);
        return frame;
    }

    private void LogNprFrameSummaryIfNeeded(ActiveNprPresetState presetState, NprContext context, StrokeFrame frame)
    {
        if (!_logNextNprFrameSummary)
        {
            return;
        }

        var preset = presetState.ActivePreset.Metadata;
        var counters = context.DebugFrame.Counters;
        StfuUiLog.Write(
            $"NPR frame {context.FrameId}: preset={preset.Id}, pipeline={presetState.ActivePreset.PipelineId}, " +
            $"meshes={context.Graph.Meshes.Count}, vertices={context.Graph.Vertices.Count}, triangles={context.Graph.Triangles.Count}, " +
            $"curves={counters.FeatureCurveCount}, visible={counters.VisibleSegmentCount}, hidden={counters.HiddenSegmentCount}, " +
            $"candidates={counters.StrokeCandidateCount}, strokes={counters.StrokeCount}, paths={frame.Paths.Count}, " +
            $"layers={context.NprFrame.Layers.Count}, tones={context.Graph.ToneSurfaces.Count}");
        if (context.NprFrame.Layers.Count > 0)
        {
            StfuUiLog.Write("NPR layers: " + string.Join(", ", context.NprFrame.Layers.Select(layer =>
                $"{layer.Id}[tone={layer.Tones.Count}, shading={layer.Shading.Count}, strokes={layer.Strokes.Count}]")));
            StfuUiLog.Write("NPR path layers: " + string.Join(", ", context.Frame.Paths
                .GroupBy(path => string.IsNullOrWhiteSpace(path.Metadata?.Layer) ? "unlayered" : path.Metadata!.Layer!)
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Key}={group.Count()}")));
        }

        foreach (var trace in context.DebugFrame.StepTraces.OrderByDescending(trace => trace.Milliseconds).Take(3))
        {
            StfuUiLog.Write(
                $"NPR step: {trace.StepName} {trace.Milliseconds:0.00}ms, input={trace.InputCount}, output={trace.OutputCount}, notes={trace.Notes}");
        }

        _logNextNprFrameSummary = false;
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

        return MeshToStrokeFrame(mesh, width, height, _camera.Camera, entity.Transform);
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
        switch (e.Key)
        {
            case Key.F1:
                SetNprPreset("technical-ink", ViewportRenderMode.Npr);
                return;
            case Key.F2:
                SetNprPreset("pencil-construction", ViewportRenderMode.Npr);
                return;
            case Key.F3:
                SetNprPreset("pen-ink-hatching", ViewportRenderMode.Npr);
                return;
            case Key.F4:
                SetNprPreset("manga-ink", ViewportRenderMode.Npr);
                return;
            case Key.F5:
                SetNprPreset("blueprint", ViewportRenderMode.Npr);
                return;
            case Key.F6:
                SetNprPreset("comic-surface", ViewportRenderMode.ComicSurface);
                return;
            case Key.D0:
            case Key.NumPad0:
                SetOverlay(DebugOverlayKind.None, "none");
                return;
            case Key.D3:
            case Key.NumPad3:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    SetOverlay(DebugOverlayKind.FeatureCurves, "feature-curves");
                    return;
                }
                break;
            case Key.D4:
            case Key.NumPad4:
                SetOverlay(DebugOverlayKind.VisibilitySegments, "visibility-segments");
                return;
            case Key.D5:
            case Key.NumPad5:
                SetOverlay(DebugOverlayKind.SalienceHeatmap, "salience-heatmap");
                return;
            case Key.D6:
            case Key.NumPad6:
                SetOverlay(DebugOverlayKind.StrokeCandidates, "stroke-candidates");
                return;
            case Key.D7:
            case Key.NumPad7:
                SetOverlay(DebugOverlayKind.ToneField, "tone-field");
                return;
            case Key.D8:
            case Key.NumPad8:
                SetOverlay(DebugOverlayKind.DirectionField, "direction-field");
                return;
            case Key.D9:
            case Key.NumPad9:
                SetOverlay(DebugOverlayKind.DensityField, "density-field");
                return;
            case Key.T:
                SetOverlay(DebugOverlayKind.TextureField, "texture-field");
                return;
            case Key.Y:
                SetOverlay(DebugOverlayKind.TemporalMatches, "temporal-matches");
                return;
            case Key.G:
                SetOverlay(DebugOverlayKind.GhostStrokes, "ghost-strokes");
                return;
            case Key.H:
                SetOverlay(DebugOverlayKind.HatchingPlan, "hatching-plan");
                return;
            case Key.M:
                SetOverlay(DebugOverlayKind.StyleMask, "style-mask");
                return;
            case Key.R:
                SetOverlay(DebugOverlayKind.MaterialRegion, "material-region");
                return;
        }

        var renderMode = e.Key switch
        {
            Key.D1 or Key.NumPad1 => ViewportRenderMode.Mesh,
            Key.D2 or Key.NumPad2 => ViewportRenderMode.Npr,
            Key.D3 or Key.NumPad3 => ViewportRenderMode.ComicSurface,
            _ => (ViewportRenderMode?)null
        };

        if (renderMode is null)
        {
            return;
        }

        if (renderMode.Value == ViewportRenderMode.ComicSurface)
        {
            ApplyPreset("comic-surface");
        }
        else if (renderMode.Value == ViewportRenderMode.Npr &&
            _activeNprPreset.ActivePreset.PipelineId == NprPipelineIds.ComicSurface)
        {
            ApplyPreset("technical-ink");
        }

        _commands.Enqueue(new SetViewportRenderModeCommand(renderMode.Value));
        _engine.Tick(_commands);
        StfuUiLog.Write($"Viewport render mode: {renderMode.Value}");
        if (renderMode.Value != ViewportRenderMode.Mesh)
        {
            var preset = _activeNprPreset.ActivePreset.Metadata;
            StfuUiLog.Write($"NPR preset: {preset.Id} ({preset.Name})");
        }

        InvalidateVisual();
        e.Handled = true;

        void SetOverlay(DebugOverlayKind overlay, string label)
        {
            _commands.Enqueue(new SetViewportDebugOverlayCommand(overlay));
            _engine.Tick(_commands);
            StfuUiLog.Write($"Viewport debug overlay: {label}");
            InvalidateVisual();
            e.Handled = true;
        }

        void SetNprPreset(string presetId, ViewportRenderMode renderMode)
        {
            ApplyPreset(presetId);
            _commands.Enqueue(new SetViewportRenderModeCommand(renderMode));
            _engine.Tick(_commands);
            StfuUiLog.Write($"Viewport render mode: {renderMode}");
            _logNextNprFrameSummary = true;
            InvalidateVisual();
            e.Handled = true;
        }

        void ApplyPreset(string presetId)
        {
            _activeNprPreset.ApplyPreset(presetId);
            _frameHistory.Reset();
            _logNextNprFrameSummary = true;
            var preset = _activeNprPreset.ActivePreset.Metadata;
            StfuUiLog.Write($"NPR preset: {preset.Id} ({preset.Name})");
        }
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

    private static StrokeFrame MeshToStrokeFrame(MeshData mesh, int width, int height, CameraState camera, Transform3D transform)
    {
        if (mesh.Vertices.Count == 0)
        {
            return StrokeFrame.Empty;
        }

        var paths = new List<StrokePath2D>(mesh.Triangles.Count * 3);
        var emittedEdges = new HashSet<long>();
        var projection = CameraProjection.Create(camera, width, height);
        var style = new StrokeStyle2D(0.55f, 1.0f, StrokeColor.Black);
        var worldPositions = mesh.Vertices
            .Select(vertex => TransformVertex(vertex.Position, transform))
            .ToArray();

        foreach (var triangle in mesh.Triangles)
        {
            if ((uint)triangle.A >= (uint)worldPositions.Length ||
                (uint)triangle.B >= (uint)worldPositions.Length ||
                (uint)triangle.C >= (uint)worldPositions.Length)
            {
                continue;
            }

            var aWorld = worldPositions[triangle.A];
            var bWorld = worldPositions[triangle.B];
            var cWorld = worldPositions[triangle.C];
            if (!IsFrontFacing(aWorld, bWorld, cWorld, camera.Position))
            {
                continue;
            }

            AddEdge(triangle.A, triangle.B);
            AddEdge(triangle.B, triangle.C);
            AddEdge(triangle.C, triangle.A);
        }

        return new StrokeFrame(width, height, paths);

        void AddEdge(int a, int b)
        {
            if ((uint)a >= (uint)mesh.Vertices.Count || (uint)b >= (uint)mesh.Vertices.Count)
            {
                return;
            }

            if (!emittedEdges.Add(CreateEdgeKey(a, b)))
            {
                return;
            }

            if (projection.TryProject(worldPositions[a], out var start) &&
                projection.TryProject(worldPositions[b], out var end))
            {
                paths.Add(StrokePath2D.Line(start, end, style));
            }
        }

        static long CreateEdgeKey(int a, int b)
        {
            var min = Math.Min(a, b);
            var max = Math.Max(a, b);
            return ((long)min << 32) | (uint)max;
        }

        static Vector3 TransformVertex(Vector3 position, Transform3D transform)
        {
            return Vector3.Transform(position * transform.Scale, CreateRotation(transform.Rotation)) + transform.Position;
        }

        static bool IsFrontFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 cameraPosition)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.LengthSquared() <= 0.0001f)
            {
                return false;
            }

            normal = Vector3.Normalize(normal);
            var center = (a + b + c) / 3f;
            var viewDirection = Vector3.Normalize(cameraPosition - center);
            return Vector3.Dot(normal, viewDirection) > 0f;
        }

        static Quaternion CreateRotation(Vector3 rotation)
        {
            return Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
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
        private const float NearClipDepth = 0.05f;
        private const float FarClipDepth = 500f;

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

            if (z < NearClipDepth || z > FarClipDepth || !float.IsFinite(z))
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

            return float.IsFinite(point.X) && float.IsFinite(point.Y);
        }
    }
}
