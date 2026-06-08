using STFU.Common.Math;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Strokes;
using STFU.UI.Bridge.Renderer;
using STFU.UI.Bridge.Session;
using STFU.UI.Styling;
using STFU.Viewport;

namespace STFU.UI;

internal readonly record struct ViewportRuntimeStatus(
    string EffectiveBackend,
    string EffectiveApi,
    string EffectivePresentation,
    string SurfaceMode,
    bool DirectPresenterAvailable,
    bool DirectSuppressed,
    bool PreferGpuPresentation,
    bool RequireGpuReadback,
    bool AllowGpuReadback,
    bool ShowDirectHost,
    bool DrawBitmap,
    string AdapterName,
    string StatusMessage,
    string LastOutputKind,
    float GpuReadbackMs);

internal readonly record struct ViewportRenderRequestBuild(
    NprRenderRequest Request,
    ViewportRuntimeStatus RuntimeStatus,
    RendererRuntimePlan RuntimePlan,
    NprFrameBudget FrameBudget,
    int ResolvedWorkerCount,
    bool UseDirectGpuPresenter);

internal sealed class ViewportRenderRequestFactory
{
    private readonly UiEngineSession _session;
    private readonly NprRenderOptimizerMode _optimizerMode;

    public ViewportRenderRequestFactory(
        UiEngineSession session,
        NprRenderOptimizerMode optimizerMode)
    {
        _session = session;
        _optimizerMode = optimizerMode;
    }

    public ViewportRenderRequestBuild Create(
        long revision,
        int width,
        int height,
        ViewportRenderMode viewportRenderMode,
        RendererRuntimePlan runtimePlan)
    {
        var contentKind = viewportRenderMode == ViewportRenderMode.Mesh
            ? NprRenderContentKind.MeshWireframe
            : NprRenderContentKind.NprPipeline;
        var renderer = _session.Workspace.Renderer;
        var runtimeStatus = new ViewportRuntimeStatus(
            runtimePlan.BackendLabel,
            runtimePlan.ApiLabel,
            runtimePlan.PresentationLabel,
            runtimePlan.SurfaceMode.ToString(),
            runtimePlan.DirectPresenterAvailable,
            runtimePlan.DirectSuppressed,
            runtimePlan.PreferGpuPresentation,
            runtimePlan.RequireGpuReadback,
            runtimePlan.AllowGpuReadback,
            runtimePlan.ShowDirectHost,
            runtimePlan.DrawBitmap,
            runtimePlan.AdapterLabel,
            runtimePlan.StatusMessage,
            LastOutputKind: string.Empty,
            GpuReadbackMs: 0f);
        var frameBudget = new NprFrameBudget(
            TargetFps: 60,
            MaxWorkerThreads: renderer.MaxRenderWorkers,
            AllowContinuousRendering: true,
            AllowDroppingOldFrames: true,
            EnableTileParallelism: renderer.EnableTileParallelism,
            TileSize: 32,
            RequireGpuReadback: runtimePlan.RequireGpuReadback,
            AllowGpuReadback: runtimePlan.AllowGpuReadback,
            PreferGpuPresentation: runtimePlan.PreferGpuPresentation,
            EnableGpuDebugLayer: false,
            EnableGpuTiming: renderer.EnableGpuTimings,
            WorkerBudgetMode: renderer.WorkerBudgetMode);
        var presetState = _session.ActivePreset;
        var debugOverlay = _session.Workspace.Viewport.DebugOverlay;
        var includeDebugFrame = contentKind == NprRenderContentKind.NprPipeline &&
            debugOverlay != DebugOverlayKind.None;
        var request = new NprRenderRequest(
            Revision: revision,
            Width: width,
            Height: height,
            ExecutionProfile: runtimePlan.EffectiveProfile,
            ContentKind: contentKind,
            Scene: _session.Engine.Scene,
            Assets: _session.Assets,
            Camera: _session.CameraRig.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: _session.EntityStyles,
            Analysis: _session.Analysis,
            FrameHistoryState: _session.FrameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: _session.FrameHistory.PeekNextFrameId(),
            TimeSeconds: revision / 60f,
            PreviousFrame: _session.FrameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: frameBudget,
            Theme: BuildTheme(),
            ShowGrid: _session.Workspace.Viewport.ShowGrid && viewportRenderMode == ViewportRenderMode.Mesh,
            IncludeDebugFrame: includeDebugFrame,
            DebugOverlay: debugOverlay,
            PipelineStrategy: runtimePlan.PipelineStrategy,
            DiagnosticsOptions: CreateViewportDiagnosticsOptions(renderer),
            OptimizerMode: _optimizerMode);

        return new ViewportRenderRequestBuild(
            request,
            runtimeStatus,
            runtimePlan,
            frameBudget,
            frameBudget.ResolveWorkerCount(),
            runtimePlan.PreferGpuPresentation);
    }

    private static NprDiagnosticsOptions CreateViewportDiagnosticsOptions(RendererSettingsViewModel renderer)
    {
        return renderer.EnableGpuTimings
            ? NprDiagnosticsOptions.InteractiveViewportTimings
            : NprDiagnosticsOptions.InteractiveViewport;
    }

    private static NprRenderTheme BuildTheme()
    {
        return UiThemeService.IsDark
            ? new NprRenderTheme(
                true,
                new StrokeColor(23, 25, 22),
                new StrokeColor(58, 64, 55),
                new StrokeColor(43, 48, 41),
                new StrokeColor(225, 229, 221))
            : new NprRenderTheme(
                false,
                new StrokeColor(245, 245, 242),
                new StrokeColor(215, 215, 210),
                new StrokeColor(232, 232, 228),
                StrokeColor.Black);
    }
}
