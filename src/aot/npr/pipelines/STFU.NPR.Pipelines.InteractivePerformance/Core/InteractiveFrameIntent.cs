using STFU.NPR.Pipelines.Abstractions;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveFrameIntent(
    long FrameId,
    int Width,
    int Height,
    FramePipelineStrategy Strategy,
    InteractiveQualityMode QualityMode,
    TimeSpan FrameBudget,
    bool CameraChanged,
    bool SceneChanged,
    bool AnimationChanged,
    bool StyleChanged,
    bool ViewportSizeChanged,
    bool DebugOverlayChanged,
    InteractiveFrameSignature Signature = default);
