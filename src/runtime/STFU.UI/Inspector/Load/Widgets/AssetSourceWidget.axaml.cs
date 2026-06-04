using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using STFU.UI.Bridge;
using STFU.UI.Bridge.Assets;

namespace STFU.UI.Inspector.Load.Widgets;

public sealed partial class AssetSourceWidget : UserControl
{
    public AssetSourceWidget()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnSourceClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AssetSourceOption source } ||
            DataContext is not WorkspaceViewModel workspace)
        {
            return;
        }

        workspace.Assets.SelectSource(source.Id);

        if (source.Id == "hard-drive")
        {
            await OpenHardDrivePicker(workspace);
            return;
        }

        if (source.Id == "public-domain")
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var window = new PublicDomainAssetExplorerWindow(workspace);
            if (owner is null)
            {
                window.Show();
            }
            else
            {
                await window.ShowDialog(owner);
            }
        }
    }

    private async Task OpenHardDrivePicker(WorkspaceViewModel workspace)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load asset",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("3D assets")
                {
                    Patterns = ["*.obj", "*.fbx", "*.glb", "*.gltf"]
                },
                FilePickerFileTypes.All
            ]
        });

        var file = files.FirstOrDefault();
        if (file?.Path.LocalPath is { Length: > 0 } path)
        {
            workspace.Assets.SelectAssetCandidate(path, "HARD DRIVE");
        }
    }
}
