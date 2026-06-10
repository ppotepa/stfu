namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public enum InteractiveOutputReadiness
{
    None,
    ProjectionReady,
    VisibilityReady,
    CandidateEdgesReady,
    StrokeCommandsReady,
    VisibleSegmentsReady,
    StrokeFrameReady,
    PreviewReady
}
