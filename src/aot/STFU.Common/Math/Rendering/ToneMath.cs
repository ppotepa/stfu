namespace STFU.Common.Math;

public static class ToneMath
{
    public static float EffectiveOpacity(float toneOpacity, float layerOpacity)
    {
        return NumericMath.Clamp01(toneOpacity * layerOpacity);
    }

    public static byte ScaleAlpha(byte sourceAlpha, float opacity)
    {
        return NumericMath.ScaleByte(sourceAlpha, opacity);
    }
}
