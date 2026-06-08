using STFU.Common.Math;
using STFU.Logging;
using STFU.Parallelism;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;

namespace STFU.UI.Bridge.Renderer;

public sealed class RendererSettingsViewModel : BindableObject
{
    private readonly UiEngineSession _session;
    private readonly RendererSettingsStore _store;
    private RendererBackendPreference _backendPreference;
    private RendererApiPreference _apiPreference;
    private RendererPresentationPreference _presentationPreference;
    private string _effectiveBackend = "AUTO";
    private string _effectiveApi = "AUTO";
    private string _effectivePresentation = "AUTO";
    private string _surfaceMode = "Bitmap";
    private string _adapterName = "Unavailable";
    private string _statusMessage = string.Empty;
    private string _lastOutputKind = string.Empty;
    private float _gpuReadbackMs;
    private bool _showRendererHud;
    private bool _enableGpuTimings;
    private WorkerBudgetMode _workerBudgetMode;
    private int _maxRenderWorkers;
    private bool _enableTileParallelism;
    private bool _suspendPersistence;
    private bool _directPresenterAvailable;
    private bool _directSuppressed;
    private bool _preferGpuPresentation;
    private bool _requireGpuReadback;
    private bool _allowGpuReadback;
    private bool _showDirectHost;
    private bool _drawBitmap;

    public RendererSettingsViewModel(
        UiEngineSession session,
        RendererSettingsStore store,
        RendererSettingsSnapshot snapshot)
    {
        _session = session;
        _store = store;

        _suspendPersistence = true;
        _backendPreference = snapshot.Backend;
        _apiPreference = snapshot.Api;
        _presentationPreference = snapshot.Presentation;
        _showRendererHud = snapshot.ShowRendererHud;
        _enableGpuTimings = snapshot.EnableGpuTimings;
        _workerBudgetMode = snapshot.WorkerBudgetMode;
        _maxRenderWorkers = NormalizeMaxRenderWorkers(snapshot.MaxRenderWorkers);
        _enableTileParallelism = snapshot.EnableTileParallelism;
        _suspendPersistence = false;

        UpdateRuntimeStatus(
            effectiveBackend: session.HasGpuRenderer ? "CPU+GPU" : "CPU",
            effectiveApi: session.HasGpuRenderer ? "DX11" : "CPU",
            effectivePresentation: session.HasGpuRenderer ? "Direct" : "Bitmap",
            surfaceMode: session.HasGpuRenderer ? "DirectCandidate" : "Bitmap",
            directPresenterAvailable: session.HasGpuRenderer,
            directSuppressed: false,
            preferGpuPresentation: session.HasGpuRenderer,
            requireGpuReadback: false,
            allowGpuReadback: !session.HasGpuRenderer,
            showDirectHost: false,
            drawBitmap: true,
            adapterName: session.GpuRenderBackend?.Info.Name ?? "Unavailable",
            statusMessage: session.HasGpuRenderer ? string.Empty : "GPU backend unavailable; using Full CPU.",
            lastOutputKind: string.Empty,
            gpuReadbackMs: 0f);
    }

    public RendererBackendPreference BackendPreference
    {
        get => _backendPreference;
        set
        {
            if (!SetProperty(ref _backendPreference, value))
            {
                return;
            }

            OnGpuSettingAvailabilityChanged();
            LogPreferenceChanged("backend", value);
            PersistIfNeeded();
        }
    }

    public RendererApiPreference ApiPreference
    {
        get => _apiPreference;
        set
        {
            if (!SetProperty(ref _apiPreference, value))
            {
                return;
            }

            LogPreferenceChanged("api", value);
            PersistIfNeeded();
        }
    }

    public RendererPresentationPreference PresentationPreference
    {
        get => _presentationPreference;
        set
        {
            if (!SetProperty(ref _presentationPreference, value))
            {
                return;
            }

            OnGpuSettingAvailabilityChanged();
            LogPreferenceChanged("presentation", value);
            PersistIfNeeded();
        }
    }

    public bool ShowRendererHud
    {
        get => _showRendererHud;
        set
        {
            if (!SetProperty(ref _showRendererHud, value))
            {
                return;
            }

            PersistIfNeeded();
        }
    }

    public bool EnableGpuTimings
    {
        get => _enableGpuTimings;
        set
        {
            if (!SetProperty(ref _enableGpuTimings, value))
            {
                return;
            }

            PersistIfNeeded();
        }
    }

    public WorkerBudgetMode WorkerBudgetMode
    {
        get => _workerBudgetMode;
        set
        {
            if (!SetProperty(ref _workerBudgetMode, value))
            {
                return;
            }

            LogPreferenceChanged("workerBudgetMode", value);
            OnPropertyChanged(nameof(ResolvedRenderWorkerCount));
            OnPropertyChanged(nameof(ParallelismSummary));
            PersistIfNeeded();
        }
    }

    public int MaxRenderWorkers
    {
        get => _maxRenderWorkers;
        set
        {
            var normalized = NormalizeMaxRenderWorkers(value);
            if (!SetProperty(ref _maxRenderWorkers, normalized))
            {
                return;
            }

            LogPreferenceChanged("maxRenderWorkers", normalized);
            OnPropertyChanged(nameof(ResolvedRenderWorkerCount));
            OnPropertyChanged(nameof(ParallelismSummary));
            PersistIfNeeded();
        }
    }

    public bool EnableTileParallelism
    {
        get => _enableTileParallelism;
        set
        {
            if (!SetProperty(ref _enableTileParallelism, value))
            {
                return;
            }

            LogPreferenceChanged("enableTileParallelism", value);
            PersistIfNeeded();
        }
    }

    public int ProcessorCount => WorkerBudget.LogicalProcessorCount;

    public int ResolvedRenderWorkerCount => WorkerBudget.Resolve(new WorkerBudgetRequest(
        Mode: WorkerBudgetMode,
        ExplicitWorkerCount: MaxRenderWorkers));

    public string ParallelismSummary => $"{ResolvedRenderWorkerCount}/{ProcessorCount} workers";

    public bool IsGpuAvailable => _session.HasGpuRenderer;

    public bool IsDirectX11Available => CanConfigureGpu;

    public bool CanConfigureGpu => IsGpuAvailable && BackendPreference != RendererBackendPreference.FullCpu;

    public bool CanConfigurePresentation => CanConfigureGpu;

    public bool CanConfigureGpuTimings => CanConfigureGpu;

    public bool CanUseDirectPresentation => CanConfigureGpu;

    public string GpuSettingsDisabledReason => CanConfigureGpu
        ? string.Empty
        : BackendPreference == RendererBackendPreference.FullCpu
            ? "GPU settings are disabled for Full CPU backend."
            : "GPU backend is unavailable.";

    public bool UseDirectViewportHost => CanConfigureGpu && PresentationPreference == RendererPresentationPreference.Direct;

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

    public void ApplyLaunchOverrides(
        RendererBackendPreference? backend,
        RendererApiPreference? api,
        RendererPresentationPreference? presentation)
    {
        _suspendPersistence = true;

        if (backend is { } backendValue)
        {
            _backendPreference = backendValue;
            OnPropertyChanged(nameof(BackendPreference));
            OnGpuSettingAvailabilityChanged();
        }

        if (api is { } apiValue)
        {
            _apiPreference = apiValue;
            OnPropertyChanged(nameof(ApiPreference));
        }

        if (presentation is { } presentationValue)
        {
            _presentationPreference = presentationValue;
            OnPropertyChanged(nameof(PresentationPreference));
            OnGpuSettingAvailabilityChanged();
        }

        _suspendPersistence = false;
    }

    public void UpdateRuntimeStatus(
        string effectiveBackend,
        string effectiveApi,
        string effectivePresentation,
        string surfaceMode,
        bool directPresenterAvailable,
        bool directSuppressed,
        bool preferGpuPresentation,
        bool requireGpuReadback,
        bool allowGpuReadback,
        bool showDirectHost,
        bool drawBitmap,
        string adapterName,
        string statusMessage,
        string lastOutputKind = "",
        float gpuReadbackMs = 0f)
    {
        EffectiveBackend = effectiveBackend;
        EffectiveApi = effectiveApi;
        EffectivePresentation = effectivePresentation;
        SurfaceMode = surfaceMode;
        DirectPresenterAvailable = directPresenterAvailable;
        DirectSuppressed = directSuppressed;
        PreferGpuPresentation = preferGpuPresentation;
        RequireGpuReadback = requireGpuReadback;
        AllowGpuReadback = allowGpuReadback;
        ShowDirectHost = showDirectHost;
        DrawBitmap = drawBitmap;
        AdapterName = string.IsNullOrWhiteSpace(adapterName) ? "Unavailable" : adapterName;
        StatusMessage = statusMessage;
        LastOutputKind = lastOutputKind;
        GpuReadbackMs = gpuReadbackMs;
        OnPropertyChanged(nameof(RequestedSummary));
        OnPropertyChanged(nameof(IsGpuAvailable));
        OnPropertyChanged(nameof(IsDirectX11Available));
        OnGpuSettingAvailabilityChanged();
    }

    private void OnGpuSettingAvailabilityChanged()
    {
        OnPropertyChanged(nameof(IsDirectX11Available));
        OnPropertyChanged(nameof(CanConfigureGpu));
        OnPropertyChanged(nameof(CanConfigurePresentation));
        OnPropertyChanged(nameof(CanConfigureGpuTimings));
        OnPropertyChanged(nameof(CanUseDirectPresentation));
        OnPropertyChanged(nameof(GpuSettingsDisabledReason));
        OnPropertyChanged(nameof(UseDirectViewportHost));
    }

    private void PersistIfNeeded()
    {
        if (_suspendPersistence)
        {
            return;
        }

        _store.Save(new RendererSettingsSnapshot(
            Backend: BackendPreference,
            Api: ApiPreference,
            Presentation: PresentationPreference,
            ShowRendererHud: ShowRendererHud,
            EnableGpuTimings: EnableGpuTimings,
            WorkerBudgetMode: WorkerBudgetMode,
            MaxRenderWorkers: MaxRenderWorkers,
            EnableTileParallelism: EnableTileParallelism));
    }

    private static int NormalizeMaxRenderWorkers(int value)
    {
        return NumericMath.Clamp(value, 0, WorkerBudget.LogicalProcessorCount);
    }

    private static void LogPreferenceChanged(string name, object value)
    {
        StfuLog.Write(
            StfuLogDomain.Ui,
            "renderer.preference.changed",
            $"{name}={value}",
            properties: new Dictionary<string, object?>
            {
                ["preference"] = name,
                ["value"] = value
            });
    }
}
