namespace STFU.NPR.Graph;

public sealed record SurfaceFlowCurve(FeatureCurve Curve)
{
    public int StableId => Curve.StableId;
    public IReadOnlyList<FeaturePoint> Points => Curve.Points;
    public float Importance => Curve.Importance;
}
