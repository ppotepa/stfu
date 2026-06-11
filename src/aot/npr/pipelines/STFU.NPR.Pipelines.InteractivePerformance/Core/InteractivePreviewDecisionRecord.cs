namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractivePreviewDecisionRecord(
    long FrameId,
    InteractivePreviewDecisionKind Decision,
    double ReadinessScore,
    int VisibleStrokeSegments,
    bool ReturnedInteractiveFrame,
    bool ReturnedReferenceFallback)
{
    public bool WasAccepted => ReturnedInteractiveFrame && Decision == InteractivePreviewDecisionKind.SelectedInteractiveFrame;
}
