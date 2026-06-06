using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using STFU.UI.Bridge;
using STFU.UI.Bridge.Assets;

namespace STFU.UI.Inspector.Load.Widgets;

public sealed partial class AssetSourceWidget : UserControl
{
    private StackPanel? _sourceOptionsHost;
    private TextBlock? _sourceStatusText;
    private WorkspaceViewModel? _workspace;

    public AssetSourceWidget()
    {
        AvaloniaXamlLoader.Load(this);
        _sourceOptionsHost = this.FindControl<StackPanel>("SourceOptionsHost");
        _sourceStatusText = this.FindControl<TextBlock>("SourceStatusText");
        DataContextChanged += (_, _) => AttachWorkspace(DataContext as WorkspaceViewModel);
        AttachedToVisualTree += (_, _) => AttachWorkspace(DataContext as WorkspaceViewModel);
    }

    private void AttachWorkspace(WorkspaceViewModel? workspace)
    {
        if (ReferenceEquals(_workspace, workspace))
        {
            RefreshSourceUi();
            return;
        }

        if (_workspace is not null)
        {
            _workspace.Assets.PropertyChanged -= OnAssetsPropertyChanged;
        }

        _workspace = workspace;

        if (_workspace is not null)
        {
            _workspace.Assets.PropertyChanged += OnAssetsPropertyChanged;
        }

        RefreshSourceUi();
    }

    private void OnAssetsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AssetPanelViewModel.SourceStatus))
        {
            RefreshSourceUi();
        }
    }

    private void RefreshSourceUi()
    {
        if (_sourceStatusText is not null)
        {
            _sourceStatusText.Text = _workspace?.Assets.SourceStatus ?? string.Empty;
        }

        if (_sourceOptionsHost is null)
        {
            return;
        }

        _sourceOptionsHost.Children.Clear();
        if (_workspace is null)
        {
            return;
        }

        foreach (var source in _workspace.Assets.SourceOptions)
        {
            var button = new Button
            {
                MinWidth = 112,
                Tag = source,
                Content = new TextBlock
                {
                    Text = source.DisplayName,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    FontSize = 11
                }
            };
            button.Classes.Add("option-pill");
            if (source.IsSelected)
            {
                button.Classes.Add("active");
            }

            button.Click += OnSourceClicked;
            _sourceOptionsHost.Children.Add(button);
        }
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
