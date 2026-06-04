using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI.Inspector.Load;

public sealed partial class LoadTabView : UserControl
{
    public LoadTabView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
