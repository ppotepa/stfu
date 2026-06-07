using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge.Session;

namespace STFU.UI;

public sealed partial class ViewportStage : UserControl
{
    private UiEngineSession? _session;
    private EngineViewportControl? _viewport;
    private DirectXViewportPresenter? _directXPresenter;
    private ViewportRuntimeController? _runtimeController;

    public ViewportStage()
    {
        InitializeComponent();
    }

    internal void Attach(UiEngineSession session, StfuUiStartupOptions startupOptions)
    {
        _runtimeController?.Dispose();
        _session = session;
        DataContext = session.Workspace;
        if (OperatingSystem.IsWindows() && session.HasGpuRenderer)
        {
            _directXPresenter = new DirectXViewportPresenter(session);
        }

        var inputController = new ViewportInputController(session);
        _viewport = new EngineViewportControl(session, startupOptions, _directXPresenter, inputController);
        var viewport = _viewport;
        _runtimeController = new ViewportRuntimeController(
            viewport,
            session.Workspace.Renderer,
            session.HasGpuRenderer,
            session.GpuRenderBackend?.Info.Name);
        if (_directXPresenter?.IsAvailable == true)
        {
            var hostGrid = new Grid();
            var directXHost = new DirectXViewportHost(
                _directXPresenter,
                session,
                viewport.RequestImmediateFrame,
                inputController);
            _runtimeController.AttachDirectHost(directXHost);
            hostGrid.Children.Add(viewport);
            hostGrid.Children.Add(directXHost);
            GetViewportHost().Content = hostGrid;
            _runtimeController.ApplyStartupState();
            return;
        }

        GetViewportHost().Content = viewport;
        _runtimeController.ApplyStartupState();
    }

    public void FocusViewport()
    {
        _viewport?.Focus();
    }

    public void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        _viewport?.HandleKeyDown(sender, e);
    }

    private async void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        if (_session is null)
        {
            return;
        }

        var window = new SettingsWindow(_session.Workspace.Renderer);
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
        {
            window.Show();
            return;
        }

        await window.ShowDialog(owner);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private ContentControl GetViewportHost()
    {
        return this.FindControl<ContentControl>("ViewportHost")
            ?? throw new InvalidOperationException("ViewportHost control is missing from ViewportStage.");
    }
}
