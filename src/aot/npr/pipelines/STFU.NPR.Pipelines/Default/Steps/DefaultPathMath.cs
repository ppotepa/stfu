using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

internal static class DefaultPathMath
{
    public static float SegmentLength(Point2D a, Point2D b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static float PathLength(IReadOnlyList<Point2D> points)
    {
        var total = 0f;
        for (var i = 1; i < points.Count; i++)
        {
            total += SegmentLength(points[i - 1], points[i]);
        }

        return total;
    }

    public static IReadOnlyList<Point2D> PartialPath(IReadOnlyList<Point2D> points, float maxLength)
    {
        if (points.Count == 0)
        {
            return [];
        }

        if (maxLength <= 0f || points.Count < 2)
        {
            return [points[0]];
        }

        var output = new List<Point2D> { points[0] };
        var remaining = maxLength;

        for (var i = 1; i < points.Count; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            var segmentLength = SegmentLength(a, b);

            if (segmentLength <= remaining)
            {
                output.Add(b);
                remaining -= segmentLength;
                continue;
            }

            var t = remaining / Math.Max(segmentLength, 1e-6f);
            output.Add(new Point2D(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t));
            break;
        }

        return output;
    }
}
