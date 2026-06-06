namespace STFU.Strokes;

public sealed record StrokeFrame(
    int Width,
    int Height,
    IReadOnlyList<StrokePath2D> Paths,
    IReadOnlyList<StrokeSegment2D>? Segments = null)
{
    public static StrokeFrame Empty { get; } = new(0, 0, []);
}
