using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge;
using STFU.UI.Bridge.Viewport;

namespace STFU.UI;

public sealed partial class TopBarView : UserControl
{
    private ToggleSwitch? _modeToggle;
    private ToggleSwitch? _themeToggle;
    private WorkspaceViewModel? _workspace;
    private bool _syncing;

    public TopBarView()
    {
        InitializeComponent();
        _modeToggle = this.FindControl<ToggleSwitch>("ModeToggle");
        _themeToggle = this.FindControl<ToggleSwitch>("ThemeToggle");

        if (_modeToggle is not null)
        {
            _modeToggle.PropertyChanged += OnModeTogglePropertyChanged;
        }

        if (_themeToggle is not null)
        {
            _themeToggle.PropertyChanged += OnThemeTogglePropertyChanged;
        }

        DataContextChanged += (_, _) => AttachWorkspace(DataContext as WorkspaceViewModel);
        AttachedToVisualTree += (_, _) => AttachWorkspace(DataContext as WorkspaceViewModel);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AttachWorkspace(WorkspaceViewModel? workspace)
    {
        if (ReferenceEquals(_workspace, workspace))
        {
            SyncFromWorkspace();
            return;
        }

        if (_workspace is not null)
        {
            _workspace.PropertyChanged -= OnWorkspacePropertyChanged;
            _workspace.Viewport.PropertyChanged -= OnViewportPropertyChanged;
        }

        _workspace = workspace;

        if (_workspace is not null)
        {
            _workspace.PropertyChanged += OnWorkspacePropertyChanged;
            _workspace.Viewport.PropertyChanged += OnViewportPropertyChanged;
        }

        SyncFromWorkspace();
    }

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WorkspaceViewModel.IsDarkTheme))
        {
            SyncFromWorkspace();
        }
    }

    private void OnViewportPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewportViewModel.IsNprModeSelected) or nameof(ViewportViewModel.RenderMode))
        {
            SyncFromWorkspace();
        }
    }

    private void SyncFromWorkspace()
    {
        _syncing = true;
        try
        {
            if (_modeToggle is not null)
            {
                _modeToggle.IsChecked = _workspace?.Viewport.IsNprModeSelected == true;
            }

            if (_themeToggle is not null)
            {
                _themeToggle.IsChecked = _workspace?.IsDarkTheme == true;
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnModeTogglePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncing || e.Property != ToggleSwitch.IsCheckedProperty || _workspace is null || _modeToggle is null)
        {
            return;
        }

        _workspace.Viewport.IsNprModeSelected = _modeToggle.IsChecked == true;
    }

    private void OnThemeTogglePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncing || e.Property != ToggleSwitch.IsCheckedProperty || _workspace is null || _themeToggle is null)
        {
            return;
        }

        _workspace.IsDarkTheme = _themeToggle.IsChecked == true;
    }
}
