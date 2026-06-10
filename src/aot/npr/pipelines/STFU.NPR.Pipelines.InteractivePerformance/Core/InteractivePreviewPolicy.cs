using STFU.NPR.Pipelines.Abstractions;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractivePreviewPolicy
{
    public static bool TrySelectInteractiveFrame(
        FramePipelineStrategyOptions options,
        InteractivePipelineResult result,
        out StrokeFrame frame,
        out string reason)
    {
        ArgumentNullException.ThrowIfNull(result);
        options ??= FramePipelineStrategyOptions.Default;

        if (options.ForceReferenceFallback)
        {
            frame = StrokeFrame.Empty;
            reason = "FramePipelineStrategyOptions.ForceReferenceFallback is enabled.";
            return false;
        }

        if (options.UseReferenceFallbackForFinalFrame)
        {
            frame = StrokeFrame.Empty;
            reason = "UseReferenceFallbackForFinalFrame is enabled; Interactive Performance is collecting preview artifacts only.";
            return false;
        }

        if (!options.EnableInteractivePreviewOutput)
        {
            frame = StrokeFrame.Empty;
            reason = "EnableInteractivePreviewOutput is disabled.";
            return false;
        }

        var frameArtifact = result.InteractiveStrokeFrameArtifact;
        if (frameArtifact is null || !frameArtifact.HasRenderableFrame)
        {
            frame = StrokeFrame.Empty;
            reason = "Interactive stroke frame artifact is missing or empty.";
            return false;
        }

        if (options.RequireToneCoverageForInteractivePreview && (result.ToneCoverage?.RegionCount ?? 0) <= 0)
        {
            frame = StrokeFrame.Empty;
            reason = "Tone coverage is required for interactive preview output but no tone regions are available.";
            return false;
        }

        frame = frameArtifact.Frame;
        reason = "Interactive stroke frame selected for final viewport output.";
        return true;
    }

    public static bool ShouldReturnReferenceFallback(
        FramePipelineStrategyOptions options,
        InteractivePipelineResult result,
        out string reason)
    {
        var selected = TrySelectInteractiveFrame(options, result, out _, out reason);
        return !selected;
    }
}
