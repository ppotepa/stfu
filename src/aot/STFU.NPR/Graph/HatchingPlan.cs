namespace STFU.NPR.Graph;

public sealed record HatchingPlan(
    int StableId,
    int RegionId,
    STFU.Strokes.Point2D Center,
    HatchLayer Primary,
    HatchLayer? Secondary,
    HatchLayer? Tertiary,
    float ToneTarget,
    float DensityTarget);
