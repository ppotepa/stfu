namespace STFU.Common.Math;

public static class StrokeMath
{
    public static float MinimumThickness(float value, float minimum = 0.2f)
    {
        return NumericMath.AtLeast(value, minimum);
    }

    public static float Opacity(float value)
    {
        return NumericMath.Clamp01(value);
    }

    public static float PressureSample(float start, float mid, float end, float t)
    {
        var clamped = NumericMath.Clamp01(t);
        if (clamped <= 0.5f)
        {
            return NumericMath.Lerp(start, mid, clamped / 0.5f);
        }

        return NumericMath.Lerp(mid, end, (clamped - 0.5f) / 0.5f);
    }
}
