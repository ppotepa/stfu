namespace STFU.NPR.Debug;

public sealed record NprDebugCounters(
    int FeatureCurveCount,
    int VisibleSegmentCount,
    int HiddenSegmentCount,
    int SalientSegmentCount,
    int StrokeCandidateCount,
    int StrokeCount,
    int GhostStrokeCount,
    int DirectTemporalMatchCount,
    int FallbackTemporalMatchCount);
