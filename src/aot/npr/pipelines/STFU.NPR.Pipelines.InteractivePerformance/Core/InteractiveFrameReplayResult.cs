namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveFrameReplayResult(
    long FrameId,
    double TotalStageMs,
    bool ReturnedInteractiveFrame,
    bool ReturnedReferenceFallback,
    InteractivePreviewDecisionKind PreviewDecision,
    InteractiveEvidenceReport Evidence);
