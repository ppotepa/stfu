using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using STFU.UI.Bridge.Renderer;

namespace STFU.UI;

public sealed partial class RendererOptionsWindow : Window
{
    private readonly RendererSettingsViewModel _renderer;

    public RendererOptionsWindow()
    {
        InitializeComponent();
        _renderer = null!;
    }

    internal RendererOptionsWindow(RendererSettingsViewModel renderer)
        : this()
    {
        _renderer = renderer;
        DataContext = renderer;
        SyncControlsFromViewModel();
    }

    private void OnBackendSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_renderer is null)
        {
            return;
        }

        _renderer.BackendPreference = GetBackendCombo().SelectedIndex switch
        {
            1 => RendererBackendPreference.FullCpu,
            2 => RendererBackendPreference.CpuDrivenGpu,
            _ => RendererBackendPreference.Auto
        };
    }

    private void OnApiChecked(object? sender, RoutedEventArgs e)
    {
        if (_renderer is null || sender is not ToggleButton { IsChecked: true } button)
        {
            return;
        }

        if (ReferenceEquals(button, GetApiDirectX11Button()))
        {
            _renderer.ApiPreference = RendererApiPreference.DirectX11;
            return;
        }

        _renderer.ApiPreference = RendererApiPreference.Auto;
    }

    private void OnPresentationSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_renderer is null)
        {
            return;
        }

        _renderer.PresentationPreference = GetPresentationCombo().SelectedIndex switch
        {
            1 => RendererPresentationPreference.Direct,
            2 => RendererPresentationPreference.Readback,
            _ => RendererPresentationPreference.Auto
        };
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SyncControlsFromViewModel()
    {
        GetBackendCombo().SelectedIndex = _renderer.BackendPreference switch
        {
            RendererBackendPreference.FullCpu => 1,
            RendererBackendPreference.CpuDrivenGpu => 2,
            _ => 0
        };

        GetPresentationCombo().SelectedIndex = _renderer.PresentationPreference switch
        {
            RendererPresentationPreference.Direct => 1,
            RendererPresentationPreference.Readback => 2,
            _ => 0
        };

        GetApiAutoButton().IsChecked = _renderer.ApiPreference != RendererApiPreference.DirectX11;
        GetApiDirectX11Button().IsChecked = _renderer.ApiPreference == RendererApiPreference.DirectX11;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private ComboBox GetBackendCombo() => this.FindControl<ComboBox>("BackendCombo")
        ?? throw new InvalidOperationException("BackendCombo is missing.");

    private ComboBox GetPresentationCombo() => this.FindControl<ComboBox>("PresentationCombo")
        ?? throw new InvalidOperationException("PresentationCombo is missing.");

    private RadioButton GetApiAutoButton() => this.FindControl<RadioButton>("ApiAutoButton")
        ?? throw new InvalidOperationException("ApiAutoButton is missing.");

    private RadioButton GetApiDirectX11Button() => this.FindControl<RadioButton>("ApiDirectX11Button")
        ?? throw new InvalidOperationException("ApiDirectX11Button is missing.");
}
