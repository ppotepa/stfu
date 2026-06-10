using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveBudgetDecision(
    InteractiveQualityMode RequestedQualityMode,
    InteractiveQualityMode ResolvedQualityMode,
    InteractiveBudgetPressure Pressure,
    double PreviousKnownFrameMs,
    double TargetFrameMs,
    int OverBudgetStreak,
    int UnderBudgetStreak,
    bool QualityChanged,
    int EffectiveMaxCandidateEdges,
    int EffectiveMaxStrokeCommands,
    int EffectiveMaxVisibleStrokeSegments,
    bool EffectiveToneDeferred)
{
    public FramePipelineStrategyOptions ApplyTo(FramePipelineStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options with
        {
            MaxInteractiveCandidateEdges = EffectiveMaxCandidateEdges,
            MaxInteractiveStrokeCommands = EffectiveMaxStrokeCommands,
            MaxInteractiveVisibleStrokeSegments = EffectiveMaxVisibleStrokeSegments,
            DeferToneCoverageWhenPreviewDoesNotRequireTone = EffectiveToneDeferred
        };
    }
}
