namespace STFU.Common.Math;

public static class NumericMath
{
    public const double Pi = global::System.Math.PI;

    public const float DegreesToRadiansFactor = MathF.PI / 180f;

    public const float RadiansToDegreesFactor = 180f / MathF.PI;

    public static float Clamp(float value, float min, float max)
    {
        return MathF.Min(max, MathF.Max(min, value));
    }

    public static int Clamp(int value, int min, int max)
    {
        return global::System.Math.Clamp(value, min, max);
    }

    public static double Clamp(double value, double min, double max)
    {
        return global::System.Math.Clamp(value, min, max);
    }

    public static float Clamp01(float value)
    {
        return global::System.Math.Clamp(value, 0f, 1f);
    }

    public static double Clamp01(double value)
    {
        return global::System.Math.Clamp(value, 0d, 1d);
    }

    public static float DegreesToRadians(float degrees)
    {
        return degrees * DegreesToRadiansFactor;
    }

    public static float RadiansToDegrees(float radians)
    {
        return radians * RadiansToDegreesFactor;
    }

    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public static bool IsNearlyMultiple(float value, float step, float epsilon)
    {
        return MathF.Abs(value % step) < epsilon;
    }

    public static float Abs(float value)
    {
        return MathF.Abs(value);
    }

    public static int Abs(int value)
    {
        return global::System.Math.Abs(value);
    }

    public static long Abs(long value)
    {
        return global::System.Math.Abs(value);
    }

    public static double Abs(double value)
    {
        return global::System.Math.Abs(value);
    }

    public static double Round(double value, int digits)
    {
        return global::System.Math.Round(value, digits);
    }

    public static double Round(double value)
    {
        return global::System.Math.Round(value);
    }

    public static double Floor(double value)
    {
        return global::System.Math.Floor(value);
    }

    public static float Floor(float value)
    {
        return MathF.Floor(value);
    }

    public static double Ceiling(double value)
    {
        return global::System.Math.Ceiling(value);
    }

    public static float Ceiling(float value)
    {
        return MathF.Ceiling(value);
    }

    public static float Sqrt(float value)
    {
        return MathF.Sqrt(value);
    }

    public static double Sqrt(double value)
    {
        return global::System.Math.Sqrt(value);
    }

    public static double Sin(double value)
    {
        return global::System.Math.Sin(value);
    }

    public static int ScaleIndex(int targetIndex, int targetCount, int sourceCount)
    {
        return global::System.Math.Clamp(
            (int)((long)targetIndex * sourceCount / global::System.Math.Max(1, targetCount)),
            0,
            global::System.Math.Max(0, sourceCount - 1));
    }

    public static byte ClampToByte(int value)
    {
        return (byte)global::System.Math.Clamp(value, 0, 255);
    }

    public static byte UnitToByte(float value)
    {
        return ClampToByte((int)MathF.Round(Clamp01(value) * 255f));
    }

    public static byte ScaleByte(byte value, float scale)
    {
        return ClampToByte((int)MathF.Round(value * scale));
    }

    public static byte SaturatingAddByte(int left, int right)
    {
        return (byte)global::System.Math.Min(255, left + right);
    }

    public static int AtLeast(int value, int minimum)
    {
        return global::System.Math.Max(minimum, value);
    }

    public static long AtLeast(long value, long minimum)
    {
        return global::System.Math.Max(minimum, value);
    }

    public static float AtLeast(float value, float minimum)
    {
        return MathF.Max(minimum, value);
    }

    public static double AtLeast(double value, double minimum)
    {
        return global::System.Math.Max(minimum, value);
    }

    public static int AtMost(int value, int maximum)
    {
        return global::System.Math.Min(maximum, value);
    }

    public static long AtMost(long value, long maximum)
    {
        return global::System.Math.Min(maximum, value);
    }

    public static float AtMost(float value, float maximum)
    {
        return MathF.Min(maximum, value);
    }

    public static double AtMost(double value, double maximum)
    {
        return global::System.Math.Min(maximum, value);
    }

    public static float InverseAtLeast(int value, int minimum = 1)
    {
        return 1f / AtLeast(value, minimum);
    }

    public static int CeilingDivide(int value, int divisor)
    {
        var safeDivisor = AtLeast(divisor, 1);
        return value <= 0 ? 0 : (value + safeDivisor - 1) / safeDivisor;
    }

    public static double FramesPerSecond(double milliseconds)
    {
        return 1000.0 / AtLeast(milliseconds, 0.001d);
    }
}
