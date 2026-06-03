namespace STFU.Strokes;

public sealed record StrokeFrame(
    int Width,
    int Height,
    IReadOnlyList<Stroke2D> Strokes)
{
    public static StrokeFrame Empty { get; } = new(0, 0, []);
}
