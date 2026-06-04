using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge.Session;

namespace STFU.UI;

public sealed partial class ViewportStage : UserControl
{
    private EngineViewportControl? _viewport;

    public ViewportStage()
    {
        InitializeComponent();
    }

    internal void Attach(UiEngineSession session, StfuUiStartupOptions startupOptions)
    {
        DataContext = session.Workspace;
        _viewport = new EngineViewportControl(session, startupOptions);
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
