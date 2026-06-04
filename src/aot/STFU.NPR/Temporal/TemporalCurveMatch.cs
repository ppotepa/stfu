namespace STFU.NPR.Temporal;

public sealed record TemporalCurveMatch(
    int StableId,
    int PreviousStableId,
    TemporalMatchKind Kind,
    float Confidence);
