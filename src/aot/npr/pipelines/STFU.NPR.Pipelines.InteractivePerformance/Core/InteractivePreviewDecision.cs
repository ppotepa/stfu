using STFU.Strokes;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractivePreviewDecision
{
    public required InteractivePreviewDecisionKind Kind { get; init; }

    public required string Reason { get; init; }

    public StrokeFrame? Frame { get; init; }

    public bool SelectedInteractiveFrame => Kind == InteractivePreviewDecisionKind.SelectedInteractiveFrame;

    public bool UsesReferenceFallback => !SelectedInteractiveFrame;

    public int FramePathCount => Frame?.Paths.Count ?? 0;

    public int FrameSegmentCount => Frame?.Segments?.Count ?? 0;

    public static InteractivePreviewDecision SelectInteractiveFrame(
        StrokeFrame frame,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return new InteractivePreviewDecision
        {
            Kind = InteractivePreviewDecisionKind.SelectedInteractiveFrame,
            Reason = reason,
            Frame = frame
        };
    }

    public static InteractivePreviewDecision UseReferenceFallback(
        InteractivePreviewDecisionKind kind,
        string reason)
    {
        if (kind == InteractivePreviewDecisionKind.SelectedInteractiveFrame)
        {
            throw new ArgumentException(
                "Use SelectInteractiveFrame for selected interactive output.",
                nameof(kind));
        }

        return new InteractivePreviewDecision
        {
            Kind = kind,
            Reason = reason
        };
    }
}
