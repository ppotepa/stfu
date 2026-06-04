using STFU.NPR.Composition;
using STFU.Strokes;

namespace STFU.NPR.Rendering;

public sealed record NprFrame(
    int Width,
    int Height,
    NprPaper Paper,
    IReadOnlyList<NprLayerFrame> Layers,
    StrokeFrame LegacyStrokes)
{
    public static NprFrame Empty { get; } = new(0, 0, NprPaper.Default, [], StrokeFrame.Empty);
}
