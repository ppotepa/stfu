namespace STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

public enum InteractiveWorkClass
{
    ReuseOnly,
    ProjectionOnly,
    VisibilityRefresh,
    StrokeCandidateRefresh,
    FullVisibleStrokeRefresh,
    ReferenceFallback
}
