using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public static class CpuStrokeSegmentBuilder
{
    public static List<CpuStrokeSegment> Build(
        IReadOnlyList<StrokePath2D> paths,
        float opacityScale,
        bool preservePathOrder = false,
        List<CpuStrokeSegment>? reuse = null)
    {
        var segments = reuse ?? new List<CpuStrokeSegment>(paths.Count);
        segments.Clear();
        segments.EnsureCapacity(paths.Count);
        var order = 0;

        if (!preservePathOrder && paths.Any(path => path.Metadata is not null))
        {
            foreach (var path in paths.OrderByDescending(path => path.Metadata?.LayerOrder ?? 100))
            {
                AppendPath(path, opacityScale, segments, ref order);
            }

            return segments;
        }

        foreach (var path in paths)
        {
            AppendPath(path, opacityScale, segments, ref order);
        }

        return segments;
    }

    private static void AppendPath(
        StrokePath2D path,
        float opacityScale,
        List<CpuStrokeSegment> output,
        ref int order)
    {
        if (path.Points.Count < 2)
        {
            return;
        }

        var dashed = string.Equals(path.Metadata?.SourceKind, "DashedHiddenStroke", StringComparison.Ordinal);

        if (path.RichPoints is { Count: > 1 } richPoints && richPoints.Count == path.Points.Count)
        {
            for (var i = 1; i < richPoints.Count; i++)
            {
                var start = richPoints[i - 1];
                var end = richPoints[i];
                var thickness = MathF.Max(0.35f, (start.Thickness + end.Thickness) * 0.5f);
                var opacity = Math.Clamp((start.Opacity + end.Opacity) * 0.5f * opacityScale, 0f, 1f);
                AppendSegment(start.Position, end.Position, path.Style.Color, thickness, opacity, dashed, output, ref order);
            }

            return;
        }

        var pathOpacity = Math.Clamp(path.Style.Opacity * opacityScale, 0f, 1f);
        for (var i = 1; i < path.Points.Count; i++)
        {
            AppendSegment(path.Points[i - 1], path.Points[i], path.Style.Color, path.Style.Thickness, pathOpacity, dashed, output, ref order);
        }
    }

    private static void AppendSegment(
        Point2D start,
        Point2D end,
        StrokeColor color,
        float thickness,
        float opacity,
        bool dashed,
        List<CpuStrokeSegment> output,
        ref int order)
    {
        if (!dashed)
        {
            output.Add(new CpuStrokeSegment(start, end, color, thickness, opacity, order++));
            return;
        }

        const float dash = 6f;
        const float gap = 4f;
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001f)
        {
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        for (var offset = 0f; offset < length; offset += dash + gap)
        {
            var a = offset;
            var b = MathF.Min(offset + dash, length);
            if (b <= a)
            {
                continue;
            }

            var s = new Point2D(start.X + ux * a, start.Y + uy * a);
            var e = new Point2D(start.X + ux * b, start.Y + uy * b);
            output.Add(new CpuStrokeSegment(s, e, color, thickness, opacity, order++));
        }
    }
}
