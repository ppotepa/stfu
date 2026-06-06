namespace STFU.Common.Math;

public static class MetricMath
{
    public static float Distance2(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static float Distance3(float ax, float ay, float az, float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static float AbsoluteDelta(float left, float right)
    {
        return MathF.Abs(left - right);
    }

    public static int AbsoluteDelta(byte left, byte right)
    {
        return global::System.Math.Abs(left - right);
    }

    public static float Max(float left, float right)
    {
        return MathF.Max(left, right);
    }

    public static int Max(int left, int right)
    {
        return global::System.Math.Max(left, right);
    }

    public static int Min(int left, int right)
    {
        return global::System.Math.Min(left, right);
    }
}
