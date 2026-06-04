using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public readonly record struct CpuStrokeSegment(
    Point2D Start,
    Point2D End,
    StrokeColor Color,
    float Thickness,
    float Opacity,
    int Order)
{
    public float MinX => MathF.Min(Start.X, End.X) - Thickness * 0.5f - 1f;

    public float MinY => MathF.Min(Start.Y, End.Y) - Thickness * 0.5f - 1f;

    public float MaxX => MathF.Max(Start.X, End.X) + Thickness * 0.5f + 1f;

    public float MaxY => MathF.Max(Start.Y, End.Y) + Thickness * 0.5f + 1f;
}
