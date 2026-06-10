namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public enum InteractivePreviewDecisionKind
{
    Unknown,
    SelectedInteractiveFrame,
    ForcedReferenceFallback,
    ReferenceFallbackRequired,
    PreviewOutputDisabled,
    MissingInteractiveStrokeFrame,
    EmptyInteractiveStrokeFrame,
    MissingToneCoverage
}
