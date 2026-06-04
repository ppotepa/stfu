using STFU.Strokes;

namespace STFU.Rendering.DirectX.Upload;

public static class DxStrokeInstanceBuilder
{
    public static List<DxStrokeInstance> Build(
        IReadOnlyList<StrokePath2D> paths,
        float opacityScale,
        bool preservePathOrder = false,
        float flags = 0f)
    {
        var instances = new List<DxStrokeInstance>(Math.Max(4, paths.Count));
        var order = 0;

        if (!preservePathOrder && paths.Any(path => path.Metadata is not null))
        {
            foreach (var path in paths.OrderByDescending(path => path.Metadata?.LayerOrder ?? 100))
            {
                AppendPath(path, opacityScale, instances, ref order, flags);
            }

            return instances;
        }

        foreach (var path in paths)
        {
            AppendPath(path, opacityScale, instances, ref order, flags);
        }

        return instances;
    }

    private static void AppendPath(
        StrokePath2D path,
        float opacityScale,
        List<DxStrokeInstance> output,
        ref int order,
        float flags)
    {
        if (path.Points.Count < 2)
        {
            return;
        }

        var dashed = string.Equals(path.Metadata?.SourceKind, "DashedHiddenStroke", StringComparison.Ordinal);

        if (path.RichPoints is { Count: > 1 } richPoints && richPoints.Count == path.Points.Count)
        {
            for (var index = 1; index < richPoints.Count; index++)
            {
                var start = richPoints[index - 1];
                var end = richPoints[index];
                var thickness = MathF.Max(0.35f, (start.Thickness + end.Thickness) * 0.5f);
                var opacity = Math.Clamp((start.Opacity + end.Opacity) * 0.5f * opacityScale, 0f, 1f);
                AppendSegment(start.Position, end.Position, path.Style.Color, thickness, opacity, dashed, output, ref order, flags);
            }

            return;
        }

        var pathOpacity = Math.Clamp(path.Style.Opacity * opacityScale, 0f, 1f);
        for (var index = 1; index < path.Points.Count; index++)
        {
            AppendSegment(path.Points[index - 1], path.Points[index], path.Style.Color, path.Style.Thickness, pathOpacity, dashed, output, ref order, flags);
        }
    }

    private static void AppendSegment(
        Point2D start,
        Point2D end,
        StrokeColor color,
        float thickness,
        float opacity,
        bool dashed,
        List<DxStrokeInstance> output,
        ref int order,
        float flags)
    {
        if (!dashed)
        {
            output.Add(Create(start, end, color, thickness, opacity, order++, flags));
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
            output.Add(Create(s, e, color, thickness, opacity, order++, flags));
        }
    }

    private static DxStrokeInstance Create(
        Point2D start,
        Point2D end,
        StrokeColor color,
        float thickness,
        float opacity,
        int order,
        float flags)
    {
        return new DxStrokeInstance(
            start.X,
            start.Y,
            end.X,
            end.Y,
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            opacity,
            MathF.Max(0.35f, thickness),
            order,
            flags);
    }
}
