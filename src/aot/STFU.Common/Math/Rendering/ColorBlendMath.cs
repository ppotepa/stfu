namespace STFU.Common.Math;

public readonly record struct Bgra32Premultiplied(byte B, byte G, byte R, byte A);

public static class ColorBlendMath
{
    public static byte Premultiply(byte color, byte alpha)
    {
        return (byte)(color * alpha / 255);
    }

    public static Bgra32Premultiplied PremultiplyBgra(byte b, byte g, byte r, byte alpha)
    {
        return new Bgra32Premultiplied(
            Premultiply(b, alpha),
            Premultiply(g, alpha),
            Premultiply(r, alpha),
            alpha);
    }

    public static uint PackBgra32Premultiplied(byte r, byte g, byte b, byte alpha)
    {
        var premul = PremultiplyBgra(b, g, r, alpha);
        return (uint)(premul.B | (premul.G << 8) | (premul.R << 16) | (premul.A << 24));
    }

    public static byte SourceOverChannel(byte sourcePremultiplied, byte destinationPremultiplied, byte sourceAlpha)
    {
        var invA = 255 - sourceAlpha;
        return NumericMath.SaturatingAddByte(sourcePremultiplied, destinationPremultiplied * invA / 255);
    }
}
