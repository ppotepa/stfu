using STFU.Assets;
using STFU.Camera;
using STFU.Engine;
using STFU.Mesh;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.Rendering.Abstractions.Backend;
using STFU.NPR.Rendering;
using STFU.NPR.Temporal;
using STFU.Rendering.Abstractions.Execution;
using STFU.Strokes;
using STFU.UI.Bridge.Camera;
using STFU.UI.Bridge.Npr;
using STFU.UI.Bridge.Presets;
using STFU.UI.Bridge.Renderer;
using STFU.UI.Bridge.Viewport;
using STFU.Viewport;

namespace STFU.UI.Bridge.Session;

public sealed class UiEngineSession
{
    public UiEngineSession(StfuEngine engine, Action<bool>? applyTheme = null)
    {
        Engine = engine;
        Commands = new UiCommandBus(engine);

        Assets = engine.Registry.GetRequired<AssetRegistry>();
        CameraRig = engine.Registry.GetRequired<CameraRig>();
        MeshFactory = engine.Registry.GetRequired<MeshFactory>();
        ActivePreset = engine.Registry.GetRequired<ActiveNprPresetState>();
        PresetRegistry = engine.Registry.GetRequired<NprPresetRegistry>();
        EntityStyles = engine.Registry.GetRequired<NprEntityStyleRegistry>();
        NprFrames = engine.Registry.GetRequired<NprFrameState>();
        Analysis = engine.Registry.GetRequired<MeshAnalysisCacheStore>();
        FrameHistory = engine.Registry.GetRequired<FrameHistoryState>();
        Debug = engine.Registry.GetRequired<NprDebugState>();
        Strokes = engine.Registry.GetRequired<StrokeState>();
        Viewport = engine.Registry.GetRequired<ViewportState>();
        RenderScheduler = engine.Registry.GetRequired<INprRenderScheduler>();
        GpuRenderBackend = engine.Registry.TryGet<IGpuRenderBackend>(out var gpuRenderBackend) ? gpuRenderBackend : null;
        RendererSettingsStore = new RendererSettingsStore();

        Workspace = new WorkspaceViewModel(this, applyTheme);
    }

    public StfuEngine Engine { get; }

    public UiCommandBus Commands { get; }

    public WorkspaceViewModel Workspace { get; }

    public AssetRegistry Assets { get; }

    public CameraRig CameraRig { get; }

    public MeshFactory MeshFactory { get; }

    public ActiveNprPresetState ActivePreset { get; }

    public NprPresetRegistry PresetRegistry { get; }

    public NprEntityStyleRegistry EntityStyles { get; }

    public NprFrameState NprFrames { get; }

    public MeshAnalysisCacheStore Analysis { get; }

    public FrameHistoryState FrameHistory { get; }

    public NprDebugState Debug { get; }

    public StrokeState Strokes { get; }

    public ViewportState Viewport { get; }

    public INprRenderScheduler RenderScheduler { get; }

    public IGpuRenderBackend? GpuRenderBackend { get; }

    public RendererSettingsStore RendererSettingsStore { get; }

    public bool HasGpuRenderer => GpuRenderBackend?.IsAvailable == true;

    public void RefreshFromEngine()
    {
        Workspace.Camera.RefreshFromEngine();
        Workspace.Viewport.RefreshFromEngine();
        Workspace.Presets.RefreshFromEngine();
        Workspace.DefaultDrawing.RefreshFromEngine();
        Workspace.RefreshPanelsFromEngine();
    }
}
