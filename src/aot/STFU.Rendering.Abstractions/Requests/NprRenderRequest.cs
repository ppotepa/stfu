using STFU.Assets;
using STFU.Camera;
using STFU.Engine.Scenes;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.NPR.Pipeline;
using STFU.NPR.Pipelines.Abstractions;
using STFU.NPR.Settings;
using STFU.NPR.Temporal;
using STFU.Rendering.Abstractions.Execution;

namespace STFU.Rendering.Abstractions.Requests;

public sealed record NprRenderRequest(
    long Revision,
    int Width,
    int Height,
    NprExecutionProfile ExecutionProfile,
    NprRenderContentKind ContentKind,
    Scene Scene,
    AssetRegistry Assets,
    CameraState Camera,
    NprSettings Settings,
    StyleGrammar Style,
    NprStyleSet StyleSet,
    NprEntityStyleRegistry EntityStyles,
    MeshAnalysisCacheStore Analysis,
    FrameHistoryState FrameHistoryState,
    INprPipeline? Pipeline,
    string ActivePresetId,
    string ActivePipelineId,
    int FrameId,
    float TimeSeconds,
    FrameHistory? PreviousFrame,
    NprQualityProfile Quality,
    NprFrameBudget Budget,
    NprRenderTheme Theme,
    bool ShowGrid,
    bool IncludeDebugFrame = true,
    DebugOverlayKind DebugOverlay = DebugOverlayKind.None,
    NprDiagnosticsOptions? DiagnosticsOptions = null,
    NprRenderOptimizerMode OptimizerMode = NprRenderOptimizerMode.Auto,
    FramePipelineStrategy PipelineStrategy = FramePipelineStrategy.ReferenceQuality);
