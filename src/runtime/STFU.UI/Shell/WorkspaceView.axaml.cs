using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge.Session;

namespace STFU.UI;

public sealed partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
    }

    internal WorkspaceView(UiEngineSession session, StfuUiStartupOptions startupOptions)
        : this()
    {
        DataContext = session.Workspace;
        GetViewportStage().Attach(session, startupOptions);
    }

    public void FocusViewport()
    {
        GetViewportStage().FocusViewport();
    }

    public void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        GetViewportStage().HandleKeyDown(sender, e);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private ViewportStage GetViewportStage()
    {
        return this.FindControl<ViewportStage>("ViewportStage")
            ?? throw new InvalidOperationException("ViewportStage control is missing from WorkspaceView.");
    }
}
