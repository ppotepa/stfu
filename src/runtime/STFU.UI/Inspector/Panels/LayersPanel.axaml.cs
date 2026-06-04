using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI;

public sealed partial class LayersPanel : UserControl
{
    public LayersPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
