using STFU.Common.Math;
using STFU.Strokes;

namespace STFU.Rendering.DirectX.Upload;

public static class DxStrokeInstanceBuilder
{
    public readonly record struct PathSortEntry(StrokePath2D Path, int OriginalIndex);

    public static List<DxStrokeInstance> Build(
        IReadOnlyList<StrokePath2D> paths,
        float opacityScale,
        List<DxStrokeInstance> output,
        List<PathSortEntry> sortScratch,
        bool preservePathOrder = false,
        float flags = 0f)
    {
        var instances = output;
        instances.Clear();
        instances.EnsureCapacity(EstimatePathInstanceCapacity(paths));
        var order = 0;

        if (!preservePathOrder && RequiresSort(paths))
        {
            sortScratch.Clear();
            sortScratch.EnsureCapacity(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                sortScratch.Add(new PathSortEntry(paths[i], i));
            }

            sortScratch.Sort(static (a, b) =>
            {
                var orderCompare = (b.Path.Metadata?.LayerOrder ?? 100).CompareTo(a.Path.Metadata?.LayerOrder ?? 100);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }

                return a.OriginalIndex.CompareTo(b.OriginalIndex);
            });

            for (var i = 0; i < sortScratch.Count; i++)
            {
                AppendPath(sortScratch[i].Path, opacityScale, instances, ref order, flags);
            }

            return instances;
        }

        for (var i = 0; i < paths.Count; i++)
        {
            AppendPath(paths[i], opacityScale, instances, ref order, flags);
        }

        return instances;
    }

    public static List<DxStrokeInstance> Build(
        IReadOnlyList<StrokeSegment2D> segments,
        float opacityScale,
        List<DxStrokeInstance> output,
        float flags = 0f)
    {
        output.Clear();
        output.EnsureCapacity(NumericMath.AtLeast(segments.Count, 4));
        var order = 0;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var opacity = NumericMath.Clamp01(segment.Style.Opacity * opacityScale);
            output.Add(Create(
                segment.Start,
                segment.End,
                segment.Style.Color,
                segment.Style.Thickness,
                opacity,
                order++,
                flags));
        }

        return output;
    }

    private static int EstimatePathInstanceCapacity(IReadOnlyList<StrokePath2D> paths)
    {
        var total = 0;
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            if (path.TryGetSegment(out _, out _))
            {
                total++;
                continue;
            }

            var pointCount = path.RichPoints is { Count: > 1 } richPoints
                ? richPoints.Count
                : path.Points.Count;
            total += NumericMath.AtLeast(pointCount - 1, 0);
        }

        return NumericMath.AtLeast(total, 4);
    }

    private static bool RequiresSort(IReadOnlyList<StrokePath2D> paths)
    {
        for (var i = 0; i < paths.Count; i++)
        {
            if (paths[i].Metadata is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendPath(
        StrokePath2D path,
        float opacityScale,
        List<DxStrokeInstance> output,
        ref int order,
        float flags)
    {
        var dashed = string.Equals(path.Metadata?.SourceKind, "DashedHiddenStroke", StringComparison.Ordinal);

        if (path.TryGetSegment(out var segmentStart, out var segmentEnd))
        {
            if (path.RichPoints is { Count: > 1 } segmentRichPoints && segmentRichPoints.Count == 2)
            {
                var start = segmentRichPoints[0];
                var end = segmentRichPoints[1];
                var thickness = NumericMath.AtLeast((start.Thickness + end.Thickness) * 0.5f, 0.35f);
                var opacity = NumericMath.Clamp01((start.Opacity + end.Opacity) * 0.5f * opacityScale);
                AppendSegment(start.Position, end.Position, path.Style.Color, thickness, opacity, dashed, output, ref order, flags);
                return;
            }

            var segmentOpacity = NumericMath.Clamp01(path.Style.Opacity * opacityScale);
            AppendSegment(segmentStart, segmentEnd, path.Style.Color, path.Style.Thickness, segmentOpacity, dashed, output, ref order, flags);
            return;
        }

        if (path.Points.Count < 2)
        {
            return;
        }

        if (path.RichPoints is { Count: > 1 } richPoints && richPoints.Count == path.Points.Count)
        {
            for (var index = 1; index < richPoints.Count; index++)
            {
                var start = richPoints[index - 1];
                var end = richPoints[index];
                var thickness = NumericMath.AtLeast((start.Thickness + end.Thickness) * 0.5f, 0.35f);
                var opacity = NumericMath.Clamp01((start.Opacity + end.Opacity) * 0.5f * opacityScale);
                AppendSegment(start.Position, end.Position, path.Style.Color, thickness, opacity, dashed, output, ref order, flags);
            }

            return;
        }

        var pathOpacity = NumericMath.Clamp01(path.Style.Opacity * opacityScale);
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
        if (!StrokeDashMath.TryCreateBasis(start.X, start.Y, end.X, end.Y, out var basis))
        {
            return;
        }

        for (var offset = 0f; offset < basis.Length; offset = StrokeDashMath.Advance(offset, dash, gap))
        {
            var a = offset;
            var b = StrokeDashMath.ClampDashEnd(offset, dash, basis.Length);
            if (b <= a)
            {
                continue;
            }

            var startPoint = StrokeDashMath.PointAtDistance(start.X, start.Y, basis, a);
            var endPoint = StrokeDashMath.PointAtDistance(start.X, start.Y, basis, b);
            var s = new Point2D(startPoint.X, startPoint.Y);
            var e = new Point2D(endPoint.X, endPoint.Y);
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
            NumericMath.AtLeast(thickness, 0.35f),
            order,
            flags);
    }
}
