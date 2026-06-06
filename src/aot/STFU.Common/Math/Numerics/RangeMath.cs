namespace STFU.Common.Math;

public static class RangeMath
{
    public static int Clamp(int value, int min, int max) => NumericMath.Clamp(value, min, max);

    public static float Clamp(float value, float min, float max) => NumericMath.Clamp(value, min, max);

    public static double Clamp(double value, double min, double max) => NumericMath.Clamp(value, min, max);

    public static float Clamp01(float value) => NumericMath.Clamp01(value);

    public static double Clamp01(double value) => NumericMath.Clamp01(value);

    public static int AtLeast(int value, int minimum) => NumericMath.AtLeast(value, minimum);

    public static float AtLeast(float value, float minimum) => NumericMath.AtLeast(value, minimum);

    public static int AtMost(int value, int maximum) => NumericMath.AtMost(value, maximum);

    public static float AtMost(float value, float maximum) => NumericMath.AtMost(value, maximum);
}
