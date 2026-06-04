namespace STFU.NPR.Temporal;

public sealed record TemporalStrokeMatch(
    int StableId,
    int PreviousStableId,
    int SourceFeatureId,
    TemporalMatchKind Kind,
    float PreviousLifetime,
    TemporalStrokeState PreviousState,
    float Confidence);
