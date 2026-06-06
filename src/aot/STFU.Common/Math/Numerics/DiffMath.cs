namespace STFU.Common.Math;

public static class DiffMath
{
    public static float AbsoluteDelta(float left, float right) => MetricMath.AbsoluteDelta(left, right);

    public static int AbsoluteDelta(byte left, byte right) => MetricMath.AbsoluteDelta(left, right);

    public static float Distance2(float ax, float ay, float bx, float by)
    {
        return MetricMath.Distance2(ax, ay, bx, by);
    }

    public static float Distance3(float ax, float ay, float az, float bx, float by, float bz)
    {
        return MetricMath.Distance3(ax, ay, az, bx, by, bz);
    }

    public static float Max(float left, float right) => MetricMath.Max(left, right);

    public static int Min(int left, int right) => MetricMath.Min(left, right);
}
