using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using STFU.Common.Math;
using STFU.Engine;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.Strokes;
using STFU.UI.Bridge.Session;
using STFU.UI.Styling;
using STFU.Viewport;

namespace STFU.UI;

public sealed class EngineViewportControl : Control
{
    private readonly UiEngineSession _session;
    private readonly ActiveNprPresetState _activeNprPreset;
    private readonly ViewportState _viewport;
    private readonly UiFrameClock _frameClock = new();
    private readonly AvaloniaBitmapPresenter _bitmapPresenter = new();
    private readonly DirectXViewportPresenter? _directXPresenter;
    private readonly ViewportRenderBridge _renderBridge;
    private Point _lastPointerPosition;
    private bool _isOrbiting;
    private bool _loggedOrbitInput;
    private bool _loggedPanInput;
    private bool _loggedFovInput;
    private bool _invalidateQueued;
    private readonly DispatcherTimer _renderTimer;

    public EngineViewportControl(StfuEngine engine)
        : this(new UiEngineSession(engine), StfuUiStartupOptions.Default)
    {
    }

    internal EngineViewportControl(UiEngineSession session, StfuUiStartupOptions startupOptions)
        : this(session, startupOptions, null)
    {
    }

    internal EngineViewportControl(
        UiEngineSession session,
        StfuUiStartupOptions startupOptions,
        DirectXViewportPresenter? directXPresenter)
    {
        _session = session;
        _activeNprPreset = session.ActivePreset;
        _viewport = session.Viewport;
        _directXPresenter = directXPresenter;
        _renderBridge = new ViewportRenderBridge(
            session,
            _bitmapPresenter,
            RequestInvalidate,
            directXPresenter);

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
        _renderTimer.Tick += (_, _) => RequestInvalidate();

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
            _session.Workspace.Presets.ApplyPreset(startupOptions.PresetId);
            var preset = _activeNprPreset.ActivePreset.Metadata;
            StfuUiLog.Write($"Startup NPR preset: {preset.Id} ({preset.Name})");
        }

        if (startupOptions.RenderMode is { } renderMode)
        {
            _session.Workspace.Viewport.RenderMode = renderMode;
            StfuUiLog.Write($"Startup viewport render mode: {renderMode}");
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var width = NumericMath.AtLeast((int)bounds.Width, 1);
        var height = NumericMath.AtLeast((int)bounds.Height, 1);

        ProcessFrame(width, height);

        if (!_renderBridge.IsDirectGpuPresenting)
        {
            _bitmapPresenter.Draw(context, bounds, ViewportPaperColor());
            DrawDebugOverlay(context, _viewport.Snapshot.DebugFrame, _viewport.DebugOverlay);
        }
    }

    internal void PumpDirectFrame()
    {
        var bounds = Bounds;
        var width = NumericMath.AtLeast((int)bounds.Width, 1);
        var height = NumericMath.AtLeast((int)bounds.Height, 1);
        ProcessFrame(width, height);
    }

    private void ProcessFrame(int width, int height)
    {
        var presentedFrame = _renderBridge.ApplyPendingResultIfAny();

        if (presentedFrame)
        {
            _session.Workspace.Viewport.PublishFrameStats(width, height, _frameClock.RecordFrame());
        }
        else
        {
            _session.Workspace.Viewport.PublishViewportSize(width, height);
        }

        _renderBridge.RequestFrame(width, height, _viewport.RenderMode);
    }

    private void RequestInvalidate()
    {
        QueueInvalidate();
    }

    private void QueueInvalidate()
    {
        if (_invalidateQueued)
        {
            return;
        }

        _invalidateQueued = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                _invalidateQueued = false;
                InvalidateVisual();
            },
            DispatcherPriority.Render);
    }

    private static Color ViewportPaperColor()
    {
        return UiThemeService.IsDark
            ? Color.FromRgb(23, 25, 22)
            : Color.FromRgb(245, 245, 242);
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
            var (red, green, blue) = ColorMath.HeatRgb(value);
            return Color.FromArgb(alpha, red, green, blue);
        }
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
            _session.Workspace.Camera.Pan((float)-delta.X * panUnitsPerPixel, (float)delta.Y * panUnitsPerPixel);

            if (!_loggedPanInput)
            {
                StfuUiLog.Write("Viewport pan input active: Ctrl + left mouse drag.");
                _loggedPanInput = true;
            }
        }
        else
        {
            const float radiansPerPixel = 0.01f;
            _session.Workspace.Camera.Orbit((float)delta.X * radiansPerPixel, (float)-delta.Y * radiansPerPixel);

            if (!_loggedOrbitInput)
            {
                StfuUiLog.Write("Viewport orbit input active: left mouse drag.");
                _loggedOrbitInput = true;
            }
        }

        RequestInvalidate();
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
        _session.Workspace.Camera.AdjustFieldOfView((float)-e.Delta.Y * degreesPerWheelStep);

        if (!_loggedFovInput)
        {
            StfuUiLog.Write("Viewport FOV input active: mouse wheel.");
            _loggedFovInput = true;
        }

        RequestInvalidate();
        e.Handled = true;
    }

    public void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F12:
                SetNprPreset("default", ViewportRenderMode.Npr);
                return;
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
            case Key.Home:
                ResetDefaultDrawing();
                return;
            case Key.End:
                FinishDefaultDrawing();
                return;
            case Key.Space:
                ToggleDefaultAutoDraw();
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
            ApplyPreset("default");
        }

        _session.Workspace.Viewport.RenderMode = renderMode.Value;
        StfuUiLog.Write($"Viewport render mode: {renderMode.Value}");
        if (renderMode.Value != ViewportRenderMode.Mesh)
        {
            var preset = _activeNprPreset.ActivePreset.Metadata;
            StfuUiLog.Write($"NPR preset: {preset.Id} ({preset.Name})");
        }

        RequestInvalidate();
        e.Handled = true;

        void SetOverlay(DebugOverlayKind overlay, string label)
        {
            _session.Workspace.Viewport.DebugOverlay = overlay;
            StfuUiLog.Write($"Viewport debug overlay: {label}");
            RequestInvalidate();
            e.Handled = true;
        }

        void SetNprPreset(string presetId, ViewportRenderMode renderModeValue)
        {
            ApplyPreset(presetId);
            _session.Workspace.Viewport.RenderMode = renderModeValue;
            StfuUiLog.Write($"Viewport render mode: {renderModeValue}");
            RequestInvalidate();
            e.Handled = true;
        }

        void ApplyPreset(string presetId)
        {
            _session.Workspace.Presets.ApplyPreset(presetId);
            var preset = _activeNprPreset.ActivePreset.Metadata;
            StfuUiLog.Write($"NPR preset: {preset.Id} ({preset.Name})");
        }
    }

    private void ResetDefaultDrawing()
    {
        if (!string.Equals(_activeNprPreset.ActivePreset.PipelineId, NprPipelineIds.Default, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _session.Workspace.DefaultDrawing.DrawProgress = 0f;
        _session.FrameHistory.Reset();
        StfuUiLog.Write("Default draw progress reset.");
        RequestInvalidate();
    }

    private void FinishDefaultDrawing()
    {
        if (!string.Equals(_activeNprPreset.ActivePreset.PipelineId, NprPipelineIds.Default, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _session.Workspace.DefaultDrawing.DrawProgress = 1f;
        _session.FrameHistory.Reset();
        StfuUiLog.Write("Default draw progress completed.");
        RequestInvalidate();
    }

    private void ToggleDefaultAutoDraw()
    {
        if (!string.Equals(_activeNprPreset.ActivePreset.PipelineId, NprPipelineIds.Default, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var drawing = _activeNprPreset.ActiveSettings.DefaultDrawing;
        _session.Workspace.DefaultDrawing.AutoDraw = !drawing.AutoDraw;
        StfuUiLog.Write($"Default auto-draw: {(drawing.AutoDraw ? "on" : "off")}");
        RequestInvalidate();
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
}
