using STFU.NPR.Debug;
using STFU.NPR.Graph;

namespace STFU.NPR.Temporal;

public sealed record PreviousFeatureCurve(
    int StableId,
    FeatureCurveKind Kind,
    FeatureCurveSource Source,
    IReadOnlyList<FeaturePoint> Points,
    IReadOnlyList<VisibilitySegment> Segments,
    SalienceScore Salience);
