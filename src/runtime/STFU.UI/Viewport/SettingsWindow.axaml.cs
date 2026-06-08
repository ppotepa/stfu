using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using STFU.Common.Math;
using STFU.Parallelism;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Renderer;

namespace STFU.UI;

public sealed partial class SettingsWindow : Window
{
    private readonly RendererSettingsViewModel _renderer;
    private SettingsDraft _draft;

    public SettingsWindow()
    {
        InitializeComponent();
        _renderer = null!;
        _draft = null!;
    }

    internal SettingsWindow(RendererSettingsViewModel renderer)
        : this()
    {
        _renderer = renderer;
        _draft = SettingsDraft.FromRenderer(renderer);
        DataContext = _draft;
        Closed += OnWindowClosed;
        renderer.PropertyChanged += OnRendererPropertyChanged;
        SyncControlsFromDraft();
    }

    private void OnBackendSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        _draft.BackendPreference = GetBackendCombo().SelectedIndex switch
        {
            1 => RendererBackendPreference.FullCpu,
            2 => RendererBackendPreference.CpuDrivenGpu,
            _ => RendererBackendPreference.Auto
        };
    }

    private void OnApiChecked(object? sender, RoutedEventArgs e)
    {
        if (_draft is null || sender is not ToggleButton { IsChecked: true } button)
        {
            return;
        }

        if (ReferenceEquals(button, GetApiDirectX11Button()))
        {
            _draft.ApiPreference = RendererApiPreference.DirectX11;
            return;
        }

        _draft.ApiPreference = RendererApiPreference.Auto;
    }

    private void OnPresentationSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        _draft.PresentationPreference = GetPresentationCombo().SelectedIndex switch
        {
            1 => RendererPresentationPreference.Direct,
            2 => RendererPresentationPreference.Readback,
            _ => RendererPresentationPreference.Auto
        };
    }

    private void OnWorkerBudgetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        _draft.WorkerBudgetMode = GetWorkerBudgetCombo().SelectedIndex switch
        {
            1 => WorkerBudgetMode.Balanced,
            2 => WorkerBudgetMode.MaxPerformance,
            3 => WorkerBudgetMode.BackgroundSafe,
            4 => WorkerBudgetMode.SingleThreadDeterministic,
            5 => WorkerBudgetMode.Benchmark,
            _ => WorkerBudgetMode.Performance
        };
    }

    private void OnApplyClicked(object? sender, RoutedEventArgs e)
    {
        ApplyDraftToRenderer();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnRendererPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        if (e.PropertyName is nameof(RendererSettingsViewModel.EffectiveBackend)
            or nameof(RendererSettingsViewModel.EffectiveApi)
            or nameof(RendererSettingsViewModel.EffectivePresentation)
            or nameof(RendererSettingsViewModel.SurfaceMode)
            or nameof(RendererSettingsViewModel.DirectPresenterAvailable)
            or nameof(RendererSettingsViewModel.DirectSuppressed)
            or nameof(RendererSettingsViewModel.PreferGpuPresentation)
            or nameof(RendererSettingsViewModel.RequireGpuReadback)
            or nameof(RendererSettingsViewModel.AllowGpuReadback)
            or nameof(RendererSettingsViewModel.ShowDirectHost)
            or nameof(RendererSettingsViewModel.DrawBitmap)
            or nameof(RendererSettingsViewModel.AdapterName)
            or nameof(RendererSettingsViewModel.StatusMessage)
            or nameof(RendererSettingsViewModel.LastOutputKind)
            or nameof(RendererSettingsViewModel.GpuReadbackMs)
            or nameof(RendererSettingsViewModel.IsGpuAvailable)
            or nameof(RendererSettingsViewModel.IsDirectX11Available)
            or nameof(RendererSettingsViewModel.CanConfigureGpu)
            or nameof(RendererSettingsViewModel.CanConfigurePresentation)
            or nameof(RendererSettingsViewModel.CanConfigureGpuTimings)
            or nameof(RendererSettingsViewModel.CanUseDirectPresentation)
            or nameof(RendererSettingsViewModel.GpuSettingsDisabledReason)
            or nameof(RendererSettingsViewModel.UseDirectViewportHost))
        {
            _draft.SyncRuntimeFrom(_renderer);
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_renderer is not null)
        {
            _renderer.PropertyChanged -= OnRendererPropertyChanged;
        }
    }

    private void ApplyDraftToRenderer()
    {
        if (_renderer is null || _draft is null)
        {
            return;
        }

        _renderer.BackendPreference = _draft.BackendPreference;
        _renderer.ApiPreference = _draft.ApiPreference;
        _renderer.PresentationPreference = _draft.PresentationPreference;
        _renderer.ShowRendererHud = _draft.ShowRendererHud;
        _renderer.EnableGpuTimings = _draft.EnableGpuTimings;
        _renderer.WorkerBudgetMode = _draft.WorkerBudgetMode;
        _renderer.MaxRenderWorkers = _draft.MaxRenderWorkers;
        _renderer.EnableTileParallelism = _draft.EnableTileParallelism;
        _draft.SyncRuntimeFrom(_renderer);
    }

    private void SyncControlsFromDraft()
    {
        GetBackendCombo().SelectedIndex = _draft.BackendPreference switch
        {
            RendererBackendPreference.FullCpu => 1,
            RendererBackendPreference.CpuDrivenGpu => 2,
            _ => 0
        };

        GetPresentationCombo().SelectedIndex = _draft.PresentationPreference switch
        {
            RendererPresentationPreference.Direct => 1,
            RendererPresentationPreference.Readback => 2,
            _ => 0
        };

        GetWorkerBudgetCombo().SelectedIndex = _draft.WorkerBudgetMode switch
        {
            WorkerBudgetMode.Balanced => 1,
            WorkerBudgetMode.MaxPerformance => 2,
            WorkerBudgetMode.BackgroundSafe => 3,
            WorkerBudgetMode.SingleThreadDeterministic => 4,
            WorkerBudgetMode.Benchmark => 5,
            _ => 0
        };

        GetApiAutoButton().IsChecked = _draft.ApiPreference != RendererApiPreference.DirectX11;
        GetApiDirectX11Button().IsChecked = _draft.ApiPreference == RendererApiPreference.DirectX11;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private ComboBox GetBackendCombo() => this.FindControl<ComboBox>("BackendCombo")
        ?? throw new InvalidOperationException("BackendCombo is missing.");

    private ComboBox GetPresentationCombo() => this.FindControl<ComboBox>("PresentationCombo")
        ?? throw new InvalidOperationException("PresentationCombo is missing.");

    private ComboBox GetWorkerBudgetCombo() => this.FindControl<ComboBox>("WorkerBudgetCombo")
        ?? throw new InvalidOperationException("WorkerBudgetCombo is missing.");

    private RadioButton GetApiAutoButton() => this.FindControl<RadioButton>("ApiAutoButton")
        ?? throw new InvalidOperationException("ApiAutoButton is missing.");

    private RadioButton GetApiDirectX11Button() => this.FindControl<RadioButton>("ApiDirectX11Button")
        ?? throw new InvalidOperationException("ApiDirectX11Button is missing.");

    private sealed class SettingsDraft : BindableObject
    {
        private RendererBackendPreference _backendPreference;
        private RendererApiPreference _apiPreference;
        private RendererPresentationPreference _presentationPreference;
        private bool _showRendererHud;
        private bool _enableGpuTimings;
        private WorkerBudgetMode _workerBudgetMode;
        private int _maxRenderWorkers;
        private bool _enableTileParallelism;
        private bool _isGpuAvailable;
        private bool _isDirectX11Available;
        private string _effectiveBackend = "AUTO";
        private string _effectiveApi = "AUTO";
        private string _effectivePresentation = "AUTO";
        private string _surfaceMode = "Bitmap";
        private string _adapterName = "Unavailable";
        private string _statusMessage = string.Empty;
        private string _lastOutputKind = string.Empty;
        private float _gpuReadbackMs;
        private bool _directPresenterAvailable;
        private bool _directSuppressed;
        private bool _preferGpuPresentation;
        private bool _requireGpuReadback;
        private bool _allowGpuReadback;
        private bool _showDirectHost;
        private bool _drawBitmap;

        private SettingsDraft(RendererSettingsViewModel renderer)
        {
            _backendPreference = renderer.BackendPreference;
            _apiPreference = renderer.ApiPreference;
            _presentationPreference = renderer.PresentationPreference;
            _showRendererHud = renderer.ShowRendererHud;
            _enableGpuTimings = renderer.EnableGpuTimings;
            _workerBudgetMode = renderer.WorkerBudgetMode;
            _maxRenderWorkers = NormalizeMaxRenderWorkers(renderer.MaxRenderWorkers);
            _enableTileParallelism = renderer.EnableTileParallelism;
            SyncRuntimeFrom(renderer);
        }

        public static SettingsDraft FromRenderer(RendererSettingsViewModel renderer)
        {
            return new SettingsDraft(renderer);
        }

        public RendererBackendPreference BackendPreference
        {
            get => _backendPreference;
            set
            {
                if (SetProperty(ref _backendPreference, value))
                {
                    OnGpuSettingAvailabilityChanged();
                }
            }
        }

        public RendererApiPreference ApiPreference
        {
            get => _apiPreference;
            set => SetProperty(ref _apiPreference, value);
        }

        public RendererPresentationPreference PresentationPreference
        {
            get => _presentationPreference;
            set
            {
                if (SetProperty(ref _presentationPreference, value))
                {
                    OnPropertyChanged(nameof(UseDirectViewportHost));
                    OnGpuSettingAvailabilityChanged();
                }
            }
        }

        public bool ShowRendererHud
        {
            get => _showRendererHud;
            set => SetProperty(ref _showRendererHud, value);
        }

        public bool EnableGpuTimings
        {
            get => _enableGpuTimings;
            set => SetProperty(ref _enableGpuTimings, value);
        }

        public WorkerBudgetMode WorkerBudgetMode
        {
            get => _workerBudgetMode;
            set
            {
                if (SetProperty(ref _workerBudgetMode, value))
                {
                    OnParallelismChanged();
                }
            }
        }

        public int MaxRenderWorkers
        {
            get => _maxRenderWorkers;
            set
            {
                var normalized = NormalizeMaxRenderWorkers(value);
                if (SetProperty(ref _maxRenderWorkers, normalized))
                {
                    OnParallelismChanged();
                }
            }
        }

        public bool EnableTileParallelism
        {
            get => _enableTileParallelism;
            set => SetProperty(ref _enableTileParallelism, value);
        }

        public int ProcessorCount => WorkerBudget.LogicalProcessorCount;

        public int ResolvedRenderWorkerCount => WorkerBudget.Resolve(new WorkerBudgetRequest(
            Mode: WorkerBudgetMode,
            ExplicitWorkerCount: MaxRenderWorkers));

        public string ParallelismSummary => $"{ResolvedRenderWorkerCount}/{ProcessorCount} workers";

        public bool IsGpuAvailable
        {
            get => _isGpuAvailable;
            private set
            {
                if (SetProperty(ref _isGpuAvailable, value))
                {
                    OnPropertyChanged(nameof(UseDirectViewportHost));
                }
            }
        }

        public bool IsDirectX11Available
        {
            get => _isDirectX11Available;
            private set => SetProperty(ref _isDirectX11Available, value);
        }

        public bool CanConfigureGpu => IsGpuAvailable && BackendPreference != RendererBackendPreference.FullCpu;

        public bool CanConfigurePresentation => CanConfigureGpu;

        public bool CanConfigureGpuTimings => CanConfigureGpu;

        public bool CanUseDirectPresentation => CanConfigureGpu;

        public string GpuSettingsDisabledReason => CanConfigureGpu
            ? string.Empty
            : BackendPreference == RendererBackendPreference.FullCpu
                ? "GPU settings are disabled for Full CPU backend."
                : "GPU backend is unavailable.";

        public bool UseDirectViewportHost => CanConfigureGpu
            && PresentationPreference == RendererPresentationPreference.Direct;

        public string EffectiveBackend
        {
            get => _effectiveBackend;
            private set
            {
                if (SetProperty(ref _effectiveBackend, value))
                {
                    OnPropertyChanged(nameof(StatusSummary));
                }
            }
        }

        public string EffectiveApi
        {
            get => _effectiveApi;
            private set
            {
                if (SetProperty(ref _effectiveApi, value))
                {
                    OnPropertyChanged(nameof(StatusSummary));
                }
            }
        }

        public string EffectivePresentation
        {
            get => _effectivePresentation;
            private set
            {
                if (SetProperty(ref _effectivePresentation, value))
                {
                    OnPropertyChanged(nameof(StatusSummary));
                }
            }
        }

        public string AdapterName
        {
            get => _adapterName;
            private set => SetProperty(ref _adapterName, value);
        }

        public string SurfaceMode
        {
            get => _surfaceMode;
            private set => SetProperty(ref _surfaceMode, value);
        }

        public bool DirectPresenterAvailable
        {
            get => _directPresenterAvailable;
            private set => SetProperty(ref _directPresenterAvailable, value);
        }

        public bool DirectSuppressed
        {
            get => _directSuppressed;
            private set => SetProperty(ref _directSuppressed, value);
        }

        public bool PreferGpuPresentation
        {
            get => _preferGpuPresentation;
            private set => SetProperty(ref _preferGpuPresentation, value);
        }

        public bool RequireGpuReadback
        {
            get => _requireGpuReadback;
            private set => SetProperty(ref _requireGpuReadback, value);
        }

        public bool AllowGpuReadback
        {
            get => _allowGpuReadback;
            private set => SetProperty(ref _allowGpuReadback, value);
        }

        public bool ShowDirectHost
        {
            get => _showDirectHost;
            private set => SetProperty(ref _showDirectHost, value);
        }

        public bool DrawBitmap
        {
            get => _drawBitmap;
            private set => SetProperty(ref _drawBitmap, value);
        }

        public string LastOutputKind
        {
            get => _lastOutputKind;
            private set => SetProperty(ref _lastOutputKind, value);
        }

        public float GpuReadbackMs
        {
            get => _gpuReadbackMs;
            private set => SetProperty(ref _gpuReadbackMs, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    OnPropertyChanged(nameof(HasStatusMessage));
                }
            }
        }

        public string StatusSummary => $"{EffectiveBackend} | {EffectiveApi} | {EffectivePresentation}";

        public string RequestedSummary => $"{BackendPreference} | {ApiPreference} | {PresentationPreference}";

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public void SyncRuntimeFrom(RendererSettingsViewModel renderer)
        {
            IsGpuAvailable = renderer.IsGpuAvailable;
            IsDirectX11Available = renderer.IsDirectX11Available;
            EffectiveBackend = renderer.EffectiveBackend;
            EffectiveApi = renderer.EffectiveApi;
            EffectivePresentation = renderer.EffectivePresentation;
            SurfaceMode = renderer.SurfaceMode;
            DirectPresenterAvailable = renderer.DirectPresenterAvailable;
            DirectSuppressed = renderer.DirectSuppressed;
            PreferGpuPresentation = renderer.PreferGpuPresentation;
            RequireGpuReadback = renderer.RequireGpuReadback;
            AllowGpuReadback = renderer.AllowGpuReadback;
            ShowDirectHost = renderer.ShowDirectHost;
            DrawBitmap = renderer.DrawBitmap;
            AdapterName = renderer.AdapterName;
            StatusMessage = renderer.StatusMessage;
            LastOutputKind = renderer.LastOutputKind;
            GpuReadbackMs = renderer.GpuReadbackMs;
            OnGpuSettingAvailabilityChanged();
        }

        private void OnParallelismChanged()
        {
            OnPropertyChanged(nameof(ResolvedRenderWorkerCount));
            OnPropertyChanged(nameof(ParallelismSummary));
        }

        private void OnGpuSettingAvailabilityChanged()
        {
            OnPropertyChanged(nameof(RequestedSummary));
            OnPropertyChanged(nameof(CanConfigureGpu));
            OnPropertyChanged(nameof(CanConfigurePresentation));
            OnPropertyChanged(nameof(CanConfigureGpuTimings));
            OnPropertyChanged(nameof(CanUseDirectPresentation));
            OnPropertyChanged(nameof(GpuSettingsDisabledReason));
            OnPropertyChanged(nameof(UseDirectViewportHost));
        }

        private static int NormalizeMaxRenderWorkers(int value)
        {
            return NumericMath.Clamp(value, 0, WorkerBudget.LogicalProcessorCount);
        }
    }
}
