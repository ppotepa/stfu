using STFU.Common.Math;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public sealed class NprStroke
{
    public NprStroke(
        int stableId,
        int featureCurveId,
        NprStrokeIntent intent,
        IReadOnlyList<Point2D> points,
        float depth,
        float shade,
        float importance,
        VisibilityState visibility,
        float tone = 0f,
        float density = 0f)
    {
        StableId = stableId;
        FeatureCurveId = featureCurveId;
        Intent = intent;
        Points = [.. points];
        Depth = depth;
        Shade = shade;
        Importance = importance;
        Visibility = visibility;
        Tone = tone;
        Density = density;
        ScreenLength = PathMath.PathLength(Points, static point => point.X, static point => point.Y);
    }

    public int StableId { get; }

    public int FeatureCurveId { get; }

    public NprStrokeIntent Intent { get; }

    public List<Point2D> Points { get; }

    public float Depth { get; }

    public float Shade { get; }

    public float Importance { get; }

    public VisibilityState Visibility { get; }

    public float Tone { get; }

    public float Density { get; }

    public float ScreenLength { get; }

    public float Thickness { get; set; } = 1.0f;

    public float Opacity { get; set; } = 1.0f;

    public StrokeColor Color { get; set; } = StrokeColor.Black;

}
