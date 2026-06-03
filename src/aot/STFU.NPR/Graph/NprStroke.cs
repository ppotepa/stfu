using STFU.Strokes;

namespace STFU.NPR.Graph;

public sealed class NprStroke
{
    public NprStroke(
        int stableId,
        NprStrokeIntent intent,
        IReadOnlyList<Point2D> points,
        float depth,
        float shade,
        float importance)
    {
        StableId = stableId;
        Intent = intent;
        Points = [.. points];
        Depth = depth;
        Shade = shade;
        Importance = importance;
        ScreenLength = MeasureLength(Points);
    }

    public int StableId { get; }

    public NprStrokeIntent Intent { get; }

    public List<Point2D> Points { get; }

    public float Depth { get; }

    public float Shade { get; }

    public float Importance { get; }

    public float ScreenLength { get; }

    public float Thickness { get; set; } = 1.0f;

    public float Opacity { get; set; } = 1.0f;

    public StrokeColor Color { get; set; } = StrokeColor.Black;

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
