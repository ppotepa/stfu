using System.Numerics;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public sealed record StrokeCandidate(
    int StableId,
    int FeatureCurveId,
    FeatureCurveKind Kind,
    NprStrokeIntent Intent,
    IReadOnlyList<Point2D> Points,
    float Depth,
    float Shade,
    float Importance,
    float Confidence,
    SalienceScore Salience,
    VisibilityState Visibility,
    float Tone,
    Vector2 Direction,
    float Density,
    HatchLayerKind? HatchLayerKind = null)
{
    public float ScreenLength => MeasureLength(Points);

    private static float MeasureLength(IReadOnlyList<Point2D> points)
    {
        var length = 0f;

        for (var index = 1; index < points.Count; index++)
        {
            var dx = points[index].X - points[index - 1].X;
            var dy = points[index].Y - points[index - 1].Y;
            length += MathF.Sqrt(dx * dx + dy * dy);
        }

        return length;
    }
}
