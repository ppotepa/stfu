using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI;

public sealed partial class LoadPanel : UserControl
{
    public LoadPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
