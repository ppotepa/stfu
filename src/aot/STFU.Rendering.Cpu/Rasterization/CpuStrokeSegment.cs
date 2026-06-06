using STFU.Common.Math;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public readonly record struct CpuStrokeSegment
{
    public CpuStrokeSegment(
        Point2D start,
        Point2D end,
        StrokeColor color,
        float thickness,
        float opacity,
        int order)
    {
        Start = start;
        End = end;
        Color = color;
        Thickness = thickness;
        Opacity = opacity;
        Order = order;

        var half = thickness * 0.5f + 1f;
        MinX = NumericMath.AtMost(start.X, end.X) - half;
        MinY = NumericMath.AtMost(start.Y, end.Y) - half;
        MaxX = NumericMath.AtLeast(start.X, end.X) + half;
        MaxY = NumericMath.AtLeast(start.Y, end.Y) + half;
    }

    public Point2D Start { get; }

    public Point2D End { get; }

    public StrokeColor Color { get; }

    public float Thickness { get; }

    public float Opacity { get; }

    public int Order { get; }

    public float MinX { get; }

    public float MinY { get; }

    public float MaxX { get; }

    public float MaxY { get; }
}
