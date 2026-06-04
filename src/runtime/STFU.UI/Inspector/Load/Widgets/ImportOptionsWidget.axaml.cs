using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace STFU.UI.Inspector.Load.Widgets;

public sealed partial class ImportOptionsWidget : UserControl
{
    public ImportOptionsWidget()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
