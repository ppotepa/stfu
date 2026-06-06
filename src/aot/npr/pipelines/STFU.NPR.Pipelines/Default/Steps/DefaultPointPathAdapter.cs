using STFU.Common.Math;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

internal static class DefaultPointPathAdapter
{
    private static readonly Func<Point2D, float> GetX = static point => point.X;
    private static readonly Func<Point2D, float> GetY = static point => point.Y;
    private static readonly Func<float, float, Point2D> CreatePoint = static (x, y) => new Point2D(x, y);

    public static float SegmentLength(Point2D a, Point2D b)
    {
        return PathMath.SegmentLength(a, b, GetX, GetY);
    }

    public static float PathLength(IReadOnlyList<Point2D> points)
    {
        return PathMath.PathLength(points, GetX, GetY);
    }

    public static IReadOnlyList<Point2D> PartialPath(IReadOnlyList<Point2D> points, float maxLength)
    {
        return PathMath.PartialPath(points, maxLength, GetX, GetY, CreatePoint);
    }
}
