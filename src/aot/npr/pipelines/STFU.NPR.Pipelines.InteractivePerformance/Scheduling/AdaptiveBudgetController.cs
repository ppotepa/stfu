using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

public sealed class AdaptiveBudgetController
{
    public InteractiveQualityMode ResolveQualityMode(
        InteractiveFrameIntent intent,
        InteractiveFrameDiagnostics previous)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(previous);

        return SelectNextQuality(
            intent.QualityMode == InteractiveQualityMode.Auto
                ? InteractiveQualityMode.BalancedViewport
                : intent.QualityMode,
            previous,
            intent.FrameBudget);
    }

    public InteractiveQualityMode SelectNextQuality(
        InteractiveQualityMode current,
        InteractiveFrameDiagnostics previous,
        TimeSpan targetFrameTime)
    {
        ArgumentNullException.ThrowIfNull(previous);

        var totalKnownMs =
            previous.ProjectionMs +
            previous.VisibilityMs +
            previous.CandidateMs +
            previous.StrokePlanMs +
            previous.TonePlanMs +
            previous.GpuUploadMs +
            previous.GpuDrawMs;

        if (totalKnownMs <= 0)
        {
            return current;
        }

        if (totalKnownMs > targetFrameTime.TotalMilliseconds * 1.25)
        {
            return current switch
            {
                InteractiveQualityMode.QualityViewport => InteractiveQualityMode.BalancedViewport,
                InteractiveQualityMode.BalancedViewport => InteractiveQualityMode.FastPreview,
                _ => InteractiveQualityMode.FastPreview
            };
        }

        if (totalKnownMs < targetFrameTime.TotalMilliseconds * 0.70)
        {
            return current switch
            {
                InteractiveQualityMode.FastPreview => InteractiveQualityMode.BalancedViewport,
                InteractiveQualityMode.BalancedViewport => InteractiveQualityMode.QualityViewport,
                _ => current
            };
        }

        return current;
    }
}
