namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public enum InteractiveOutputHealthStatus
{
    Unknown = 0,
    NoInteractiveArtifacts = 1,
    ProjectionOnly = 2,
    VisibleGeometry = 3,
    StrokeDataReady = 4,
    PreviewCandidateReady = 5,
    ReturningReferenceFallback = 6,
    ReturningInteractivePreview = 7
}
