namespace STFU.Common.Math;

public readonly record struct StrokeSegmentProjection(
    float T,
    float ClosestX,
    float ClosestY,
    float DistanceSquared);

public static class StrokeRasterCoverageMath
{
    public const float DefaultDegenerateEpsilonSquared = 0.000001f;

    public static bool TryProjectPointToSegment(
        float ax,
        float ay,
        float bx,
        float by,
        float px,
        float py,
        out StrokeSegmentProjection projection,
        float degenerateEpsilonSquared = DefaultDegenerateEpsilonSquared)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var lenSq = dx * dx + dy * dy;
        if (lenSq <= degenerateEpsilonSquared)
        {
            projection = default;
            return false;
        }

        var t = NumericMath.Clamp01(((px - ax) * dx + (py - ay) * dy) / lenSq);
        var closestX = ax + dx * t;
        var closestY = ay + dy * t;
        var ddx = px - closestX;
        var ddy = py - closestY;
        projection = new StrokeSegmentProjection(t, closestX, closestY, ddx * ddx + ddy * ddy);
        return true;
    }

    public static float Coverage(
        float distanceSquared,
        float radius,
        float softness,
        bool antialias)
    {
        if (!antialias)
        {
            return 1f;
        }

        return NumericMath.Clamp01((radius + softness - NumericMath.Sqrt(distanceSquared)) / softness);
    }
}
