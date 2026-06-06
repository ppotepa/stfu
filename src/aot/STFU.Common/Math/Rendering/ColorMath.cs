namespace STFU.Common.Math;

public static class ColorMath
{
    public static (byte R, byte G, byte B) HeatRgb(float value)
    {
        var clamped = NumericMath.Clamp01(value);
        var red = (byte)(220f * (1f - clamped) + 20f * clamped);
        var green = (byte)(40f + 175f * clamped);
        var blue = (byte)(45f + 35f * (1f - clamped));
        return (red, green, blue);
    }
}
