using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge.Session;
using STFU.UI.Styling;

namespace STFU.UI;

public sealed partial class MainWindow : Window
{
    private readonly WorkspaceView _workspace;

    public MainWindow()
        : this(StfuUiStartupOptions.Default)
    {
    }

    internal MainWindow(StfuUiStartupOptions startupOptions)
    {
        InitializeComponent();

        var session = new UiEngineSession(StfuRuntimeBootstrap.CreateEngine(), UiThemeService.Apply);
        DataContext = session.Workspace;

        _workspace = new WorkspaceView(session, startupOptions);
        GetWorkspaceHost().Content = _workspace;

        KeyDown += HandleKeyDown;
        Opened += (_, _) =>
        {
            StfuUiLog.Write("Main window opened.");
            _workspace.FocusViewport();
        };
        Closed += (_, _) => StfuUiLog.Write("Main window closed.");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        _workspace.HandleKeyDown(sender, e);
    }

    private ContentControl GetWorkspaceHost()
    {
        return this.FindControl<ContentControl>("WorkspaceHost")
            ?? throw new InvalidOperationException("WorkspaceHost control is missing from MainWindow.");
    }
}
