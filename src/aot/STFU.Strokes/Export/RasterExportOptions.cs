namespace STFU.Strokes.Export;

public sealed record RasterExportOptions(
    int Width,
    int Height,
    StrokeColor BackgroundColor,
    float Scale)
{
    public static RasterExportOptions Default { get; } = new(
        800,
        600,
        new StrokeColor(255, 255, 255),
        1f);
}
