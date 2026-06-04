using STFU.Logging;
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
    private string _adapterName = "Unavailable";
    private string _statusMessage = string.Empty;
    private bool _showRendererHud;
    private bool _enableGpuTimings;
    private bool _suspendPersistence;

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
        _suspendPersistence = false;

        UpdateRuntimeStatus(
            effectiveBackend: session.HasGpuRenderer ? "CPU+GPU" : "CPU",
            effectiveApi: session.HasGpuRenderer ? "DX11" : "CPU",
            effectivePresentation: session.HasGpuRenderer ? "Direct" : "Bitmap",
            adapterName: session.GpuRenderBackend?.Info.Name ?? "Unavailable",
            statusMessage: session.HasGpuRenderer ? string.Empty : "GPU backend unavailable; using Full CPU.");
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

            OnPropertyChanged(nameof(UseDirectViewportHost));
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

    public bool IsGpuAvailable => _session.HasGpuRenderer;

    public bool IsDirectX11Available => _session.HasGpuRenderer;

    public bool UseDirectViewportHost => IsGpuAvailable && PresentationPreference == RendererPresentationPreference.Direct;

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
            OnPropertyChanged(nameof(UseDirectViewportHost));
        }

        _suspendPersistence = false;
    }

    public void UpdateRuntimeStatus(
        string effectiveBackend,
        string effectiveApi,
        string effectivePresentation,
        string adapterName,
        string statusMessage)
    {
        EffectiveBackend = effectiveBackend;
        EffectiveApi = effectiveApi;
        EffectivePresentation = effectivePresentation;
        AdapterName = string.IsNullOrWhiteSpace(adapterName) ? "Unavailable" : adapterName;
        StatusMessage = statusMessage;
        OnPropertyChanged(nameof(IsGpuAvailable));
        OnPropertyChanged(nameof(IsDirectX11Available));
        OnPropertyChanged(nameof(UseDirectViewportHost));
    }

    private void PersistIfNeeded()
    {
        if (_suspendPersistence)
        {
            return;
        }

        _store.Save(new RendererSettingsSnapshot(
            BackendPreference,
            ApiPreference,
            PresentationPreference,
            ShowRendererHud,
            EnableGpuTimings));
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
