namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveFrameReplayInput(
    long FrameId,
    double ProjectionMs,
    double VisibilityMs,
    double CandidateMs,
    double StrokePlanMs,
    double TonePlanMs,
    bool HasPreviewCandidate,
    bool HasToneCoverage,
    int VisibleStrokeSegments)
{
    public double TotalStageMs => ProjectionMs + VisibilityMs + CandidateMs + StrokePlanMs + TonePlanMs;
}
