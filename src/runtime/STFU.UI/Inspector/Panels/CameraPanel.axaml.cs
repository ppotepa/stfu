using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI;

public sealed partial class CameraPanel : UserControl
{
    public CameraPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
