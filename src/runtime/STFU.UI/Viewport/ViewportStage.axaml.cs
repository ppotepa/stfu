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

    public ViewportStage()
    {
        InitializeComponent();
    }

    internal void Attach(UiEngineSession session, StfuUiStartupOptions startupOptions)
    {
        _session = session;
        DataContext = session.Workspace;
        if (OperatingSystem.IsWindows() && session.HasGpuRenderer)
        {
            _directXPresenter = new DirectXViewportPresenter(session);
        }

        _viewport = new EngineViewportControl(session, startupOptions, _directXPresenter);
        if (_directXPresenter?.IsAvailable == true)
        {
            var hostGrid = new Grid();
            var directXHost = new DirectXViewportHost(_directXPresenter, session, () => _viewport?.PumpDirectFrame());
            var renderer = session.Workspace.Renderer;
            directXHost.IsVisible = renderer.UseDirectViewportHost;
            renderer.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(renderer.UseDirectViewportHost) or nameof(renderer.PresentationPreference))
                {
                    directXHost.IsVisible = renderer.UseDirectViewportHost;
                }
            };
            hostGrid.Children.Add(directXHost);
            hostGrid.Children.Add(_viewport);
            GetViewportHost().Content = hostGrid;
            return;
        }

        GetViewportHost().Content = _viewport;
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
