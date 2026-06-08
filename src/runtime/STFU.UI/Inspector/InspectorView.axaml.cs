using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using STFU.UI.Inspector.Scene;

namespace STFU.UI;

public sealed partial class InspectorView : UserControl
{
    private bool _sceneTabAttached;

    public InspectorView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => AttachSceneTabView();
        DataContextChanged += (_, _) => AttachSceneTabView();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AttachSceneTabView()
    {
        if (_sceneTabAttached)
        {
            return;
        }

        var host = FindInspectorPanelHost();
        if (host is null)
        {
            return;
        }

        if (host.Children.OfType<SceneTabView>().Any())
        {
            _sceneTabAttached = true;
            return;
        }

        var sceneTab = new SceneTabView();
        sceneTab.Bind(IsVisibleProperty, new Binding("Inspector.IsSceneActive"));
        host.Children.Insert(ResolveSceneInsertIndex(host), sceneTab);
        _sceneTabAttached = true;
    }

    private Panel? FindInspectorPanelHost()
    {
        var namedHost = this.GetLogicalDescendants()
            .OfType<Panel>()
            .FirstOrDefault(panel =>
                string.Equals(panel.Name, "PanelStack", StringComparison.Ordinal) ||
                string.Equals(panel.Name, "InspectorPanelStack", StringComparison.Ordinal) ||
                string.Equals(panel.Name, "InspectorContentStack", StringComparison.Ordinal));

        if (namedHost is not null)
        {
            return namedHost;
        }

        return this.GetLogicalDescendants()
            .OfType<Panel>()
            .Where(panel => panel.Children.OfType<Control>().Any(IsKnownInspectorContent))
            .OrderByDescending(panel => panel.Children.OfType<Control>().Count(IsKnownInspectorContent))
            .FirstOrDefault();
    }

    private static int ResolveSceneInsertIndex(Panel host)
    {
        for (var index = 0; index < host.Children.Count; index++)
        {
            var childName = host.Children[index].GetType().Name;
            if (childName is "LoadTabView" or "LoadPanel")
            {
                return index + 1;
            }
        }

        return host.Children.Count;
    }

    private static bool IsKnownInspectorContent(Control control)
    {
        return control.GetType().Name is
            "LoadTabView" or
            "LoadPanel" or
            "GeneralPanel" or
            "CameraPanel" or
            "NprPanel" or
            "LayersPanel" or
            "DebugPanel" or
            "ExportPanel";
    }
}
