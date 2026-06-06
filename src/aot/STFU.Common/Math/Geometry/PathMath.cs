namespace STFU.Common.Math;

public static class PathMath
{
    public static float SegmentLength<TPoint>(
        TPoint a,
        TPoint b,
        Func<TPoint, float> getX,
        Func<TPoint, float> getY)
    {
        return Geometry2D.SegmentLength(getX(a), getY(a), getX(b), getY(b));
    }

    public static float PathLength<TPoint>(
        IReadOnlyList<TPoint> points,
        Func<TPoint, float> getX,
        Func<TPoint, float> getY)
    {
        var total = 0f;
        for (var i = 1; i < points.Count; i++)
        {
            total += SegmentLength(points[i - 1], points[i], getX, getY);
        }

        return total;
    }

    public static IReadOnlyList<TPoint> PartialPath<TPoint>(
        IReadOnlyList<TPoint> points,
        float maxLength,
        Func<TPoint, float> getX,
        Func<TPoint, float> getY,
        Func<float, float, TPoint> createPoint)
    {
        if (points.Count == 0)
        {
            return [];
        }

        if (maxLength <= 0f || points.Count < 2)
        {
            return [points[0]];
        }

        var output = new List<TPoint> { points[0] };
        var remaining = maxLength;

        for (var i = 1; i < points.Count; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            var ax = getX(a);
            var ay = getY(a);
            var bx = getX(b);
            var by = getY(b);
            var segmentLength = Geometry2D.SegmentLength(ax, ay, bx, by);

            if (segmentLength <= remaining)
            {
                output.Add(b);
                remaining -= segmentLength;
                continue;
            }

            var t = Geometry2D.SegmentInterpolationT(remaining, segmentLength);
            var point = Geometry2D.LerpPoint(ax, ay, bx, by, t);
            output.Add(createPoint(point.X, point.Y));
            break;
        }

        return output;
    }
}
