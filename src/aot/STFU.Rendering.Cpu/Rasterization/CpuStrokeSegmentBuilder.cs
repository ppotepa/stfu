using STFU.Common.Math;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public static class CpuStrokeSegmentBuilder
{
    public readonly record struct PathSortEntry(StrokePath2D Path, int OriginalIndex);

    public static List<CpuStrokeSegment> Build(
        IReadOnlyList<StrokePath2D> paths,
        float opacityScale,
        bool preservePathOrder = false,
        List<CpuStrokeSegment>? reuse = null,
        List<PathSortEntry>? sortScratch = null)
    {
        var segments = reuse ?? new List<CpuStrokeSegment>(paths.Count);
        segments.Clear();
        segments.EnsureCapacity(paths.Count);
        var order = 0;

        if (!preservePathOrder && RequiresSort(paths))
        {
            var scratch = sortScratch ?? new List<PathSortEntry>(paths.Count);
            scratch.Clear();
            scratch.EnsureCapacity(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                scratch.Add(new PathSortEntry(paths[i], i));
            }

            scratch.Sort(static (a, b) =>
            {
                var orderCompare = (b.Path.Metadata?.LayerOrder ?? 100).CompareTo(a.Path.Metadata?.LayerOrder ?? 100);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }

                return a.OriginalIndex.CompareTo(b.OriginalIndex);
            });

            for (var i = 0; i < scratch.Count; i++)
            {
                AppendPath(scratch[i].Path, opacityScale, segments, ref order);
            }

            return segments;
        }

        for (var i = 0; i < paths.Count; i++)
        {
            AppendPath(paths[i], opacityScale, segments, ref order);
        }

        return segments;
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
        List<CpuStrokeSegment> output,
        ref int order)
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
                AppendSegment(start.Position, end.Position, path.Style.Color, thickness, opacity, dashed, output, ref order);
                return;
            }

            var segmentOpacity = NumericMath.Clamp01(path.Style.Opacity * opacityScale);
            AppendSegment(segmentStart, segmentEnd, path.Style.Color, path.Style.Thickness, segmentOpacity, dashed, output, ref order);
            return;
        }

        if (path.Points.Count < 2)
        {
            return;
        }

        if (path.RichPoints is { Count: > 1 } richPoints && richPoints.Count == path.Points.Count)
        {
            for (var i = 1; i < richPoints.Count; i++)
            {
                var start = richPoints[i - 1];
                var end = richPoints[i];
                var thickness = NumericMath.AtLeast((start.Thickness + end.Thickness) * 0.5f, 0.35f);
                var opacity = NumericMath.Clamp01((start.Opacity + end.Opacity) * 0.5f * opacityScale);
                AppendSegment(start.Position, end.Position, path.Style.Color, thickness, opacity, dashed, output, ref order);
            }

            return;
        }

        var pathOpacity = NumericMath.Clamp01(path.Style.Opacity * opacityScale);
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
            output.Add(new CpuStrokeSegment(s, e, color, thickness, opacity, order++));
        }
    }
}
