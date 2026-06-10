using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using STFU.Common.Math;
using STFU.Engine;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.NPR.Pipelines.Abstractions;
using STFU.Strokes;
using STFU.UI.Bridge.Session;
using STFU.UI.Styling;
using STFU.UI.Viewport;
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
    private readonly ViewportInputController _inputController;
    private readonly ViewportSurfaceRouter _surfaceRouter;
    private readonly ViewportFrameLoop _frameLoop;
    private Point _lastPointerPosition;
    private bool _isOrbiting;
    private bool _invalidateQueued;
    private bool _presentQueued;
    private bool _lastDirectPresentationSuppressed;
    private bool _lastDirectGpuPresenting;

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
        DirectXViewportPresenter? directXPresenter,
        ViewportInputController? inputController = null)
    {
        _session = session;
        _activeNprPreset = session.ActivePreset;
        _viewport = session.Viewport;
        _directXPresenter = directXPresenter;
        _inputController = inputController ?? new ViewportInputController(session);
        _renderBridge = new ViewportRenderBridge(
            session,
            _bitmapPresenter,
            RequestPresent,
            RequestDeferredFrame,
            directXPresenter);
        _surfaceRouter = new ViewportSurfaceRouter(GetAvaloniaRenderSize);
        _frameLoop = new ViewportFrameLoop(TickFrame);

        ApplyStartupOptions(startupOptions);

        Focusable = true;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
        PointerWheelChanged += OnPointerWheelChanged;

        AttachedToVisualTree += (_, _) =>
        {
            if (!_frameLoop.IsRunning)
            {
                _frameLoop.Start();
                StfuUiLog.Write("Viewport render loop started.");
            }
        };

        DetachedFromVisualTree += (_, _) =>
        {
            if (_frameLoop.IsRunning)
            {
                _frameLoop.Stop();
                StfuUiLog.Write("Viewport render loop stopped.");
            }
        };
    }

    internal event Action<bool>? DirectPresentationSuppressionChanged;

    internal event Action? PresentationStateChanged;

    internal bool IsDirectPresentationSuppressed => _renderBridge.IsDirectPresentationSuppressed;

    internal ViewportSurfaceMode SurfaceMode => _surfaceRouter.Mode;

    internal bool ShouldShowDirectHost => _surfaceRouter.ShowDirectHost;

    internal bool IsDirectGpuPresenting => _renderBridge.IsDirectGpuPresenting;

    internal void ApplyRuntimePlan(RendererRuntimePlan plan)
    {
        var previousMode = _surfaceRouter.Mode;
        _surfaceRouter.ApplyPlan(plan);

        if (_surfaceRouter.Mode != previousMode)
        {
            PresentationStateChanged?.Invoke();
            QueueInvalidate();
        }
    }

    internal void SetDirectSurfaceSizeProvider(Func<(int Width, int Height)> sizeProvider)
    {
        _surfaceRouter.SetDirectSizeProvider(sizeProvider);
    }

    internal void RequestImmediateFrame()
    {
        _frameLoop.RequestImmediateTick();
        QueueInvalidate();
    }

    internal void ResetDirectPresentationFallback()
    {
        _renderBridge.ResetDirectPresentationFallback();
        PublishPresentationStateIfChanged();
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

        if (_surfaceRouter.DrawBitmap)
        {
            _bitmapPresenter.Draw(context, bounds, ViewportPaperColor());
            DrawDebugOverlay(context, _viewport.Snapshot.DebugFrame, _viewport.DebugOverlay);
        }

        DrawPipelineStatusHud(context, bounds);
    }

    internal void PumpDirectFrame()
    {
        RequestImmediateFrame();
    }

    internal void PumpDirectFrame(int width, int height)
    {
        RequestImmediateFrame();
    }

    private void TickFrame()
    {
        var (width, height) = _surfaceRouter.ResolveRenderSize(_renderBridge.IsDirectPresentationSuppressed);
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
        PublishPresentationStateIfChanged();

        if (presentedFrame)
        {
            QueueInvalidate(isRenderCompletion: true);
        }
    }

    private (int Width, int Height) GetAvaloniaRenderSize()
    {
        var bounds = Bounds;
        return (
            NumericMath.AtLeast((int)bounds.Width, 1),
            NumericMath.AtLeast((int)bounds.Height, 1));
    }

    private void PublishPresentationStateIfChanged()
    {
        var suppressed = _renderBridge.IsDirectPresentationSuppressed;
        var directPresenting = _renderBridge.IsDirectGpuPresenting;
        var modeBefore = _surfaceRouter.Mode;

        if (suppressed == _lastDirectPresentationSuppressed &&
            directPresenting == _lastDirectGpuPresenting &&
            modeBefore == _surfaceRouter.Mode)
        {
            return;
        }

        _lastDirectPresentationSuppressed = suppressed;
        _lastDirectGpuPresenting = directPresenting;
        DirectPresentationSuppressionChanged?.Invoke(suppressed);
        PresentationStateChanged?.Invoke();
    }

    private void RequestInvalidate()
    {
        _frameLoop.RequestImmediateTick();
        QueueInvalidate();
    }

    private void RequestPresent()
    {
        QueueInvalidate(isRenderCompletion: true);
    }

    private void RequestDeferredFrame()
    {
        _frameLoop.RequestImmediateTick();
    }

    private void QueueInvalidate(bool isRenderCompletion = false)
    {
        if (!isRenderCompletion && _invalidateQueued)
        {
            return;
        }

        if (isRenderCompletion && _presentQueued)
        {
            return;
        }

        if (isRenderCompletion)
        {
            _presentQueued = true;
        }
        else
        {
            _invalidateQueued = true;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                _invalidateQueued = false;
                _presentQueued = false;
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

    private void DrawPipelineStatusHud(DrawingContext context, Rect bounds)
    {
        var renderer = _session.Workspace.Renderer;
        if (!renderer.ShowRendererHud)
        {
            return;
        }

        var pipelineLabel = FramePipelineStrategyDisplay.GetDisplayName(renderer.PipelineStrategy);
        var fallbackLabel = renderer.PipelineStrategy == FramePipelineStrategy.InteractivePerformance
            ? "Reference Quality"
            : "no";
        var outputKind = string.IsNullOrWhiteSpace(renderer.LastOutputKind)
            ? "pending"
            : renderer.LastOutputKind;
        var presentationLabel = renderer.PreferGpuPresentation && !renderer.DrawBitmap
            ? "Direct GPU"
            : renderer.RequireGpuReadback
                ? "Readback"
                : renderer.DrawBitmap
                    ? "Bitmap"
                    : renderer.EffectivePresentation;
        var readbackLabel = renderer.RequireGpuReadback
            ? "readback required"
            : renderer.AllowGpuReadback
                ? "readback allowed"
                : "no readback";
        var status = string.IsNullOrWhiteSpace(renderer.StatusMessage)
            ? "status: OK"
            : $"status: {renderer.StatusMessage}";

        var text =
            $"Pipeline: {pipelineLabel} | Fallback: {fallbackLabel}{Environment.NewLine}" +
            $"Runtime: {renderer.EffectiveBackend} / {presentationLabel} / {renderer.SurfaceMode}{Environment.NewLine}" +
            $"Output: {outputKind} | {readbackLabel}{Environment.NewLine}" +
            status;

        var foreground = new SolidColorBrush(UiThemeService.IsDark
            ? Color.FromRgb(232, 236, 226)
            : Color.FromRgb(28, 31, 27));
        var background = new SolidColorBrush(UiThemeService.IsDark
            ? Color.FromArgb(205, 8, 10, 8)
            : Color.FromArgb(215, 255, 255, 252));
        var border = new Pen(new SolidColorBrush(UiThemeService.IsDark
            ? Color.FromArgb(160, 118, 134, 112)
            : Color.FromArgb(165, 190, 190, 180)), 1);

        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            foreground);

        const double padding = 8;
        var origin = new Point(bounds.X + 14, bounds.Y + 14);
        var panelRect = new Rect(
            origin.X - padding,
            origin.Y - padding,
            formattedText.Width + padding * 2,
            formattedText.Height + padding * 2);

        context.DrawRectangle(background, border, panelRect);
        context.DrawText(formattedText, origin);
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

        if (_inputController.MoveCamera(delta, e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            RequestInvalidate();
        }

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
        if (_inputController.ZoomCamera(e.Delta.Y))
        {
            RequestInvalidate();
        }

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
