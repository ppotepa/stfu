using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge;

namespace STFU.UI.Inspector.Load.Widgets;

public sealed partial class RecentsWidget : UserControl
{
    public RecentsWidget()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnRecentDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel workspace &&
            workspace.Assets.LoadAssetCommand.CanExecute(null))
        {
            workspace.Assets.LoadAssetCommand.Execute(null);
            e.Handled = true;
        }
    }
}
