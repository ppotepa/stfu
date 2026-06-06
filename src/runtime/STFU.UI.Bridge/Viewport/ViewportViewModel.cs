using STFU.Common.Math;
using STFU.NPR.Debug;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;
using STFU.Viewport.Commands;

namespace STFU.UI.Bridge.Viewport;

public sealed class ViewportViewModel : BindableObject
{
    private readonly STFU.Viewport.ViewportState _viewport;
    private readonly UiCommandBus _commands;
    private int _width;
    private int _height;
    private STFU.Viewport.ViewportRenderMode _renderMode;
    private DebugOverlayKind _debugOverlay;
    private double _fps;
    private bool _showGrid = true;

    public ViewportViewModel(STFU.Viewport.ViewportState viewport, UiCommandBus commands)
    {
        _viewport = viewport;
        _commands = commands;
        RefreshFromEngine();
    }

    public int Width
    {
        get => _width;
        private set => SetProperty(ref _width, NumericMath.AtLeast(value, 1));
    }

    public int Height
    {
        get => _height;
        private set => SetProperty(ref _height, NumericMath.AtLeast(value, 1));
    }

    public double Fps
    {
        get => _fps;
        private set => SetProperty(ref _fps, value);
    }

    public STFU.Viewport.ViewportRenderMode RenderMode
    {
        get => _renderMode;
        set
        {
            if (!SetProperty(ref _renderMode, value))
            {
                return;
            }

            _commands.Execute(new SetViewportRenderModeCommand(value), $"SetViewportRenderModeCommand({value})");
            RaiseRenderModeDerivedProperties();
            RefreshFromEngine();
        }
    }

    public DebugOverlayKind DebugOverlay
    {
        get => _debugOverlay;
        set
        {
            if (!SetProperty(ref _debugOverlay, value))
            {
                return;
            }

            _commands.Execute(new SetViewportDebugOverlayCommand(value), $"SetViewportDebugOverlayCommand({value})");
            RefreshFromEngine();
        }
    }

    public string SizeLabel => $"{Width} x {Height}";

    public string ModeLabel => RenderMode.ToString();

    public bool IsNprModeSelected
    {
        get => RenderMode == STFU.Viewport.ViewportRenderMode.Npr;
        set
        {
            var target = value
                ? STFU.Viewport.ViewportRenderMode.Npr
                : STFU.Viewport.ViewportRenderMode.Mesh;

            if (RenderMode != target)
            {
                RenderMode = target;
            }
        }
    }

    public bool IsMeshMode => RenderMode == STFU.Viewport.ViewportRenderMode.Mesh;

    public bool IsNprMode => RenderMode == STFU.Viewport.ViewportRenderMode.Npr;

    public bool IsComicSurfaceMode => RenderMode == STFU.Viewport.ViewportRenderMode.ComicSurface;

    public bool ShowGrid
    {
        get => _showGrid;
        set => SetProperty(ref _showGrid, value);
    }

    public void PublishFrameStats(int width, int height, double fps)
    {
        Width = width;
        Height = height;
        Fps = fps;
        OnPropertyChanged(nameof(SizeLabel));
    }

    public void PublishViewportSize(int width, int height)
    {
        Width = width;
        Height = height;
        OnPropertyChanged(nameof(SizeLabel));
    }

    public void RefreshFromEngine()
    {
        Width = _viewport.Width;
        Height = _viewport.Height;
        SetProperty(ref _renderMode, _viewport.RenderMode, nameof(RenderMode));
        SetProperty(ref _debugOverlay, _viewport.DebugOverlay, nameof(DebugOverlay));
        RaiseRenderModeDerivedProperties();
        OnPropertyChanged(nameof(SizeLabel));
    }

    private void RaiseRenderModeDerivedProperties()
    {
        OnPropertyChanged(nameof(ModeLabel));
        OnPropertyChanged(nameof(IsNprModeSelected));
        OnPropertyChanged(nameof(IsMeshMode));
        OnPropertyChanged(nameof(IsNprMode));
        OnPropertyChanged(nameof(IsComicSurfaceMode));
    }
}
