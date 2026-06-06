namespace STFU.Common.Math;

public readonly record struct StrokeDashBasis(float Length, float UnitX, float UnitY);

public static class StrokeDashMath
{
    public static bool TryCreateBasis(
        float startX,
        float startY,
        float endX,
        float endY,
        out StrokeDashBasis basis,
        float minimumLength = 0.001f)
    {
        var dx = endX - startX;
        var dy = endY - startY;
        var length = Geometry2D.SegmentLength(startX, startY, endX, endY);
        if (length <= minimumLength)
        {
            basis = default;
            return false;
        }

        basis = new StrokeDashBasis(length, dx / length, dy / length);
        return true;
    }

    public static float ClampDashEnd(float offset, float dashLength, float totalLength)
    {
        return NumericMath.AtMost(offset + dashLength, totalLength);
    }

    public static float Advance(float offset, float dashLength, float gapLength)
    {
        return offset + dashLength + gapLength;
    }

    public static (float X, float Y) PointAtDistance(float startX, float startY, StrokeDashBasis basis, float distance)
    {
        return (startX + basis.UnitX * distance, startY + basis.UnitY * distance);
    }
}
