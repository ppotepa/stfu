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

public sealed record StrokePath2D(
    IReadOnlyList<Point2D> Points,
    StrokeStyle2D Style)
{
    public static StrokePath2D Line(Point2D start, Point2D end, StrokeStyle2D style)
    {
        return new StrokePath2D([start, end], style);
    }
}
