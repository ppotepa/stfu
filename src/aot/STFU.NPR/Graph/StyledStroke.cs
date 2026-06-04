using STFU.Common.Primitives;
using STFU.Strokes;
using STFU.NPR.Temporal;

namespace STFU.NPR.Graph;

public sealed class StyledStroke
{
    public StyledStroke(
        int stableId,
        int featureCurveId,
        FeatureCurveKind kind,
        NprStrokeIntent intent,
        IReadOnlyList<Point2D> points,
        float depth,
        float shade,
        float importance,
        VisibilityState visibility,
        float tone = 0f,
        float density = 0f,
        HatchLayerKind? hatchLayerKind = null,
        EntityId entityId = default)
    {
        StableId = stableId;
        FeatureCurveId = featureCurveId;
        Kind = kind;
        Intent = intent;
        Points = [.. points];
        Depth = depth;
        Shade = shade;
        Importance = importance;
        Visibility = visibility;
        Tone = tone;
        Density = density;
        HatchLayerKind = hatchLayerKind;
        EntityId = entityId;
        ScreenLength = MeasureLength(Points);
    }

    public int StableId { get; }

    public int FeatureCurveId { get; }

    public FeatureCurveKind Kind { get; }

    public NprStrokeIntent Intent { get; }

    public List<Point2D> Points { get; }

    public float Depth { get; }

    public float Shade { get; }

    public float Importance { get; }

    public VisibilityState Visibility { get; }

    public float Tone { get; }

    public float Density { get; }

    public HatchLayerKind? HatchLayerKind { get; }

    public EntityId EntityId { get; }

    public float ScreenLength { get; }

    public float Thickness { get; set; } = 1.0f;

    public float Opacity { get; set; } = 1.0f;

    public StrokeColor Color { get; set; } = StrokeColor.Black;

    public TemporalStrokeState TemporalState { get; set; } = TemporalStrokeState.FadingIn;

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
