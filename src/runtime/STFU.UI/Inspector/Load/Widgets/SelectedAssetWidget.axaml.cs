using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI.Inspector.Load.Widgets;

public sealed partial class SelectedAssetWidget : UserControl
{
    public SelectedAssetWidget()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
