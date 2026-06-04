using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Visibility;
using STFU.Strokes;

internal static class VisibilityFixture
{
    public static IReadOnlyList<VisibilitySegment> ResolveHorizontalOcclusion(NprContext context, IVisibilityResolver? resolver = null)
    {
        resolver ??= new SampleVisibilityResolver();
        var curve = FeatureCurve.FromLine(
            9901,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            new FeaturePoint(new Point2D(10f, 50f), 0.5f),
            new FeaturePoint(new Point2D(90f, 50f), 0.5f),
            FeatureCurveSource.None,
            shade: 0.35f,
            importance: 0.8f,
            flags: FeatureCurveFlags.Generated);
        return resolver.Resolve(context, [curve]);
    }
}
