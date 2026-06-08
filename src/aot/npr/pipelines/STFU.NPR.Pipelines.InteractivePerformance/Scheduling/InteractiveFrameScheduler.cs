using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

public sealed class InteractiveFrameScheduler
{
    public InteractiveWorkClass SelectWork(
        InteractiveFrameIntent intent,
        InteractiveFrameDiagnostics previous)
    {
        if (intent.DebugOverlayChanged)
        {
            return InteractiveWorkClass.ReferenceFallback;
        }

        if (intent.SceneChanged || intent.ViewportSizeChanged)
        {
            return InteractiveWorkClass.FullVisibleStrokeRefresh;
        }

        if (intent.AnimationChanged)
        {
            return InteractiveWorkClass.VisibilityRefresh;
        }

        if (intent.CameraChanged)
        {
            return intent.QualityMode switch
            {
                InteractiveQualityMode.FastPreview => InteractiveWorkClass.ProjectionOnly,
                InteractiveQualityMode.BalancedViewport => InteractiveWorkClass.VisibilityRefresh,
                InteractiveQualityMode.QualityViewport => InteractiveWorkClass.StrokeCandidateRefresh,
                _ => InteractiveWorkClass.VisibilityRefresh
            };
        }

        if (intent.StyleChanged)
        {
            return InteractiveWorkClass.StrokeCandidateRefresh;
        }

        return InteractiveWorkClass.ReuseOnly;
    }
}
