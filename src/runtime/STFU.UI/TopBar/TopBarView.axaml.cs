using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI;

public sealed partial class TopBarView : UserControl
{
    public TopBarView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
