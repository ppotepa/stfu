using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI;

public sealed partial class GeneralPanel : UserControl
{
    public GeneralPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
