using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuGridRasterizer
{
    private readonly CpuStrokeRasterizer _strokes = new();

    public void DrawGrid(PixelSurface target, NprRenderTheme theme, NprQualityProfile quality, NprFrameBudget budget)
    {
        var paths = new List<StrokePath2D>();
        for (var x = 0f; x <= target.Width; x += 24f)
        {
            var color = Math.Abs(x % 96f) < 0.01f ? theme.GridMajorColor : theme.GridMinorColor;
            paths.Add(StrokePath2D.Line(new Point2D(x, 0), new Point2D(x, target.Height), new StrokeStyle2D(1f, 1f, color)));
        }

        for (var y = 0f; y <= target.Height; y += 24f)
        {
            var color = Math.Abs(y % 96f) < 0.01f ? theme.GridMajorColor : theme.GridMinorColor;
            paths.Add(StrokePath2D.Line(new Point2D(0, y), new Point2D(target.Width, y), new StrokeStyle2D(1f, 1f, color)));
        }

        _strokes.DrawPaths(target, paths, 1f, quality, budget);
    }
}
