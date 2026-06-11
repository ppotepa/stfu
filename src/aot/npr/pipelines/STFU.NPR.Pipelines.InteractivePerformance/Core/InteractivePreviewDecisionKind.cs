namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public enum InteractivePreviewDecisionKind
{
    Unknown,
    SelectedInteractiveFrame,
    PreviewReady = SelectedInteractiveFrame,
    ForcedReferenceFallback,
    ReferenceFallbackRequired,
    PreviewOutputDisabled,
    MissingInteractiveStrokeFrame,
    EmptyInteractiveStrokeFrame,
    OutputReadinessTooLow,
    StrokeSegmentBudgetExceeded,
    MissingToneCoverage
}
