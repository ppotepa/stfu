using System.Numerics;
using STFU.Common.Math;
using STFU.Common.Primitives;
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
    HatchLayerKind? HatchLayerKind = null,
    EntityId EntityId = default)
{
    public float ScreenLength => PathMath.PathLength(Points, static point => point.X, static point => point.Y);
}
