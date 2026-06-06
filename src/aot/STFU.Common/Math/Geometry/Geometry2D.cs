using System.Numerics;

namespace STFU.Common.Math;

public static class Geometry2D
{
    public static float SegmentLength(float ax, float ay, float bx, float by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static float SegmentLengthSquared(float ax, float ay, float bx, float by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        return dx * dx + dy * dy;
    }

    public static float SegmentInterpolationT(float remainingLength, float segmentLength, float epsilon = 1e-6f)
    {
        return remainingLength / NumericMath.AtLeast(segmentLength, epsilon);
    }

    public static (float X, float Y) LerpPoint(float ax, float ay, float bx, float by, float t)
    {
        return (
            NumericMath.Lerp(ax, bx, t),
            NumericMath.Lerp(ay, by, t));
    }

    public static float SignedTriangleArea(
        float ax,
        float ay,
        float bx,
        float by,
        float cx,
        float cy)
    {
        return ((bx - ax) * (cy - ay) - (by - ay) * (cx - ax)) * 0.5f;
    }

    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
        return a + (b - a) * t;
    }

    public static int QuantizeCoordinate(float value, float quantum)
    {
        return (int)MathF.Round(value / quantum);
    }

    public static (int X, int Y) QuantizePoint(float x, float y, float quantum)
    {
        return (QuantizeCoordinate(x, quantum), QuantizeCoordinate(y, quantum));
    }

    public static double PerpendicularDistanceSquared(
        double px,
        double py,
        double ax,
        double ay,
        double bx,
        double by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        if (global::System.Math.Abs(dx) < double.Epsilon && global::System.Math.Abs(dy) < double.Epsilon)
        {
            var pointDx = px - ax;
            var pointDy = py - ay;
            return pointDx * pointDx + pointDy * pointDy;
        }

        var numerator = global::System.Math.Abs(dy * px - dx * py + bx * ay - by * ax);
        return numerator * numerator / (dx * dx + dy * dy);
    }
}
