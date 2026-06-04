using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge;

namespace STFU.UI.Inspector.Load;

public sealed partial class PublicDomainAssetExplorerWindow : Window
{
    private readonly WorkspaceViewModel? _workspace;

    public PublicDomainAssetExplorerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public PublicDomainAssetExplorerWindow(WorkspaceViewModel workspace)
    {
        _workspace = workspace;
        AvaloniaXamlLoader.Load(this);
    }

    private void OnUseAsset(object? sender, RoutedEventArgs e)
    {
        var list = this.FindControl<ListBox>("AssetList");
        if (_workspace is not null && list?.SelectedItem is ListBoxItem { Tag: string fileName })
        {
            _workspace.Assets.SelectAssetCandidate(ResolveAssetPath(fileName), "PUBLIC DOMAIN");
        }

        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string ResolveAssetPath(string fileName)
    {
        foreach (var root in EnumerateAssetRoots())
        {
            var path = Path.GetFullPath(Path.Combine(root, "assets", fileName));
            if (File.Exists(path))
            {
                return path;
            }
        }

        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "assets", fileName));
    }

    private static IEnumerable<string> EnumerateAssetRoots()
    {
        yield return Environment.CurrentDirectory;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }
}
