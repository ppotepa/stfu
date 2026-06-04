using STFU.Strokes;

namespace STFU.NPR.Composition;

public sealed record NprPaper(
    StrokeColor Color,
    float Opacity)
{
    public static NprPaper Default { get; } = new(new StrokeColor(232, 226, 213), 1f);
}
