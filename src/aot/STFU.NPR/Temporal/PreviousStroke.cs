using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Temporal;

public sealed record PreviousStroke(
    int StableId,
    int SourceFeatureId,
    NprStrokeIntent Intent,
    StrokePath2D Path,
    float Lifetime,
    float LastSeenTime,
    TemporalStrokeState State);
