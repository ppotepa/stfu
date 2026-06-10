using STFU.NPR.Pipelines.Abstractions;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractivePreviewPolicy
{
    public static InteractivePreviewDecision Decide(
        FramePipelineStrategyOptions options,
        InteractivePipelineResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        options ??= FramePipelineStrategyOptions.Default;

        if (options.ForceReferenceFallback)
        {
            return InteractivePreviewDecision.UseReferenceFallback(
                InteractivePreviewDecisionKind.ForcedReferenceFallback,
                "FramePipelineStrategyOptions.ForceReferenceFallback is enabled.");
        }

        if (options.UseReferenceFallbackForFinalFrame)
        {
            return InteractivePreviewDecision.UseReferenceFallback(
                InteractivePreviewDecisionKind.ReferenceFallbackRequired,
                "UseReferenceFallbackForFinalFrame is enabled; Interactive Performance is collecting preview artifacts only.");
        }

        if (!options.EnableInteractivePreviewOutput)
        {
            return InteractivePreviewDecision.UseReferenceFallback(
                InteractivePreviewDecisionKind.PreviewOutputDisabled,
                "EnableInteractivePreviewOutput is disabled.");
        }

        var frameArtifact = result.InteractiveStrokeFrameArtifact;
        if (frameArtifact is null)
        {
            return InteractivePreviewDecision.UseReferenceFallback(
                InteractivePreviewDecisionKind.MissingInteractiveStrokeFrame,
                "Interactive stroke frame artifact is missing.");
        }

        if (!frameArtifact.HasRenderableFrame)
        {
            return InteractivePreviewDecision.UseReferenceFallback(
                InteractivePreviewDecisionKind.EmptyInteractiveStrokeFrame,
                "Interactive stroke frame artifact is empty.");
        }

        if (options.RequireToneCoverageForInteractivePreview && (result.ToneCoverage?.RegionCount ?? 0) <= 0)
        {
            return InteractivePreviewDecision.UseReferenceFallback(
                InteractivePreviewDecisionKind.MissingToneCoverage,
                "Tone coverage is required for interactive preview output but no tone regions are available.");
        }

        return InteractivePreviewDecision.SelectInteractiveFrame(
            frameArtifact.Frame,
            "Interactive stroke frame selected for final viewport output.");
    }

    public static bool TrySelectInteractiveFrame(
        FramePipelineStrategyOptions options,
        InteractivePipelineResult result,
        out StrokeFrame frame,
        out string reason)
    {
        var decision = Decide(options, result);
        reason = decision.Reason;

        if (decision.SelectedInteractiveFrame && decision.Frame is not null)
        {
            frame = decision.Frame;
            return true;
        }

        frame = StrokeFrame.Empty;
        return false;
    }

    public static bool ShouldReturnReferenceFallback(
        FramePipelineStrategyOptions options,
        InteractivePipelineResult result,
        out string reason)
    {
        var decision = Decide(options, result);
        reason = decision.Reason;
        return decision.UsesReferenceFallback;
    }
}
