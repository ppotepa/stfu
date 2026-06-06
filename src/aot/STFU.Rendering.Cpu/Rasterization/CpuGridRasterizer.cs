using STFU.Common.Math;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuGridRasterizer
{
    private readonly CpuStrokeRasterizer _strokes = new();

    public void DrawGrid(
        PixelSurface target,
        NprRenderTheme theme,
        NprQualityProfile quality,
        NprFrameBudget budget,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var gridSegments = workspace.GridSegments;
        gridSegments.Clear();
        var order = 0;
        for (var x = 0f; x <= target.Width; x += 24f)
        {
            var color = NumericMath.IsNearlyMultiple(x, 96f, 0.01f) ? theme.GridMajorColor : theme.GridMinorColor;
            gridSegments.Add(new CpuStrokeSegment(
                new Point2D(x, 0),
                new Point2D(x, target.Height),
                color,
                1f,
                1f,
                order++));
        }

        for (var y = 0f; y <= target.Height; y += 24f)
        {
            var color = NumericMath.IsNearlyMultiple(y, 96f, 0.01f) ? theme.GridMajorColor : theme.GridMinorColor;
            gridSegments.Add(new CpuStrokeSegment(
                new Point2D(0, y),
                new Point2D(target.Width, y),
                color,
                1f,
                1f,
                order++));
        }

        _strokes.DrawSegments(target, gridSegments, quality, budget, workspace, cancellationToken);
    }
}
