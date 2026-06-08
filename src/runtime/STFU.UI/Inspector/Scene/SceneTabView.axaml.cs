using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI.Inspector.Scene;

public sealed partial class SceneTabView : UserControl
{
    public SceneTabView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
