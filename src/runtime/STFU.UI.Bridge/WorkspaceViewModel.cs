using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using STFU.UI.Bridge.Assets;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Camera;
using STFU.UI.Bridge.Debug;
using STFU.UI.Bridge.Export;
using STFU.UI.Bridge.Inspector;
using STFU.UI.Bridge.Layers;
using STFU.UI.Bridge.Npr;
using STFU.UI.Bridge.Presets;
using STFU.UI.Bridge.Scene;
using STFU.UI.Bridge.Session;
using STFU.UI.Bridge.Viewport;
using STFU.Viewport;

namespace STFU.UI.Bridge;

public sealed class WorkspaceViewModel : BindableObject
{
    private readonly UiEngineSession _session;
    private readonly Action<bool>? _applyTheme;
    private bool _stableRandom = true;
    private bool _isDarkTheme;

    public WorkspaceViewModel(UiEngineSession session, Action<bool>? applyTheme = null)
    {
        _session = session;
        _applyTheme = applyTheme;
        Inspector = new InspectorViewModel();
        Camera = new CameraViewModel(session.CameraRig, session.Commands);
        Viewport = new ViewportViewModel(session.Viewport, session.Commands);
        Presets = new PresetViewModel(session.PresetRegistry, session.ActivePreset, session.FrameHistory, session.Commands);
        DefaultDrawing = new DefaultDrawingViewModel(session.ActivePreset, session.Commands);
        Scene = new ScenePanelViewModel(session);
        Assets = new AssetPanelViewModel(session, Scene);
        Layers = new LayerStackViewModel(session);
        Debug = new DebugPanelViewModel(session);
        Export = new ExportPanelViewModel(session);

        SetMeshModeCommand = new RelayCommand(() => Viewport.RenderMode = ViewportRenderMode.Mesh);
        SetNprModeCommand = new RelayCommand(() => Viewport.RenderMode = ViewportRenderMode.Npr);
        SetComicSurfaceModeCommand = new RelayCommand(() => Viewport.RenderMode = ViewportRenderMode.ComicSurface);
        ResetCameraCommand = Camera.ResetCommand;
        ResetTabCommand = new RelayCommand(() => session.Commands.Record($"Reset {Inspector.ActiveTab} tab requested"));
        ApplySettingsCommand = new RelayCommand(() => session.Commands.Record("Apply active inspector state -> RequestRenderCommand()"));
        ExportSvgCommand = Export.ExportSvgCommand;

        Viewport.PropertyChanged += OnViewportChanged;
        Presets.PropertyChanged += OnPresetChanged;
        session.Commands.Log.CollectionChanged += (_, _) => OnPropertyChanged(nameof(LastCommandText));
    }

    public InspectorViewModel Inspector { get; }

    public CameraViewModel Camera { get; }

    public ViewportViewModel Viewport { get; }

    public PresetViewModel Presets { get; }

    public DefaultDrawingViewModel DefaultDrawing { get; }

    public AssetPanelViewModel Assets { get; }

    public ScenePanelViewModel Scene { get; }

    public LayerStackViewModel Layers { get; }

    public DebugPanelViewModel Debug { get; }

    public ExportPanelViewModel Export { get; }

    public ObservableCollection<UiCommandLogEntry> CommandLog => Presets.Commands.Log;

    public string WindowTitle => $"STFU {Viewport.Width}x{Viewport.Height} {Viewport.Fps:0.0} FPS";

    public string LastCommandText => CommandLog.Count == 0 ? "CommandBuffer idle" : CommandLog[0].Text;

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (!SetProperty(ref _isDarkTheme, value))
            {
                return;
            }

            _applyTheme?.Invoke(value);
            OnPropertyChanged(nameof(ThemeModeLabel));
        }
    }

    public string ThemeModeLabel => IsDarkTheme ? "DARK" : "LIGHT";

    public bool StableRandom
    {
        get => _stableRandom;
        set
        {
            if (!SetProperty(ref _stableRandom, value))
            {
                return;
            }

            _session.Commands.Record($"NprSettings deterministic seed {(value ? "enabled" : "disabled")}");
            Debug.SetDeterministic(value);
        }
    }

    public ICommand SetMeshModeCommand { get; }

    public ICommand SetNprModeCommand { get; }

    public ICommand SetComicSurfaceModeCommand { get; }

    public ICommand ResetCameraCommand { get; }

    public ICommand ResetTabCommand { get; }

    public ICommand ApplySettingsCommand { get; }

    public ICommand ExportSvgCommand { get; }

    public void RefreshPanelsFromEngine()
    {
        Scene.RefreshFromEngine();
        Assets.RefreshFromEngine();
        Layers.RefreshFromEngine();
        Debug.RefreshFromEngine();
    }

    private void OnViewportChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewportViewModel.Width) or nameof(ViewportViewModel.Height) or nameof(ViewportViewModel.Fps))
        {
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    private void OnPresetChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PresetViewModel.ActivePresetId))
        {
            DefaultDrawing.RefreshFromEngine();
            Layers.RefreshFromEngine();
        }
    }
}
