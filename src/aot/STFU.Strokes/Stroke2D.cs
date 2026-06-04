namespace STFU.Strokes;

public readonly record struct Point2D(float X, float Y);

public readonly record struct StrokeColor(byte R, byte G, byte B)
{
    public static StrokeColor Black { get; } = new(20, 20, 20);
}

public readonly record struct StrokeStyle2D(
    float Thickness,
    float Opacity,
    StrokeColor Color)
{
    public static StrokeStyle2D Default { get; } = new(1.0f, 1.0f, StrokeColor.Black);
}

public readonly record struct Stroke2D(
    Point2D Start,
    Point2D End,
    float Thickness);

public readonly record struct StrokePoint2D(
    Point2D Position,
    float Thickness,
    float Opacity,
    float Pressure = 1f)
{
    public static StrokePoint2D FromPoint(Point2D position, StrokeStyle2D style, float pressure = 1f)
    {
        return new StrokePoint2D(position, style.Thickness, style.Opacity, pressure);
    }
}

public readonly record struct StrokeMetadata(
    int StableId,
    string? Layer,
    string? SourceKind,
    string? Intent = null,
    int? SourceFeatureId = null,
    int? SourceSegmentId = null,
    string? Visibility = null,
    string? StyleId = null,
    string? Variant = null,
    int LayerOrder = 0,
    int? EntityId = null);

public sealed record StrokePath2D(
    IReadOnlyList<Point2D> Points,
    StrokeStyle2D Style,
    IReadOnlyList<StrokePoint2D>? RichPoints = null,
    StrokeMetadata? Metadata = null)
{
    public static StrokePath2D Line(Point2D start, Point2D end, StrokeStyle2D style)
    {
        var points = new[] { start, end };
        var richPoints = new[]
        {
            StrokePoint2D.FromPoint(start, style),
            StrokePoint2D.FromPoint(end, style)
        };

        return new StrokePath2D(points, style, richPoints);
    }
}
