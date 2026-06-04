using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI.Inspector.Load.Widgets;

public sealed partial class LoadActionWidget : UserControl
{
    public LoadActionWidget()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
