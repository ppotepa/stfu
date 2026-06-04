using STFU.Strokes;

namespace STFU.Rendering.Abstractions.Requests;

public sealed record NprRenderTheme(
    bool IsDark,
    StrokeColor PaperColor,
    StrokeColor GridMajorColor,
    StrokeColor GridMinorColor,
    StrokeColor MeshStrokeColor)
{
    public static NprRenderTheme Light { get; } = new(
        false,
        new StrokeColor(245, 245, 242),
        new StrokeColor(215, 215, 210),
        new StrokeColor(232, 232, 228),
        StrokeColor.Black);
}
