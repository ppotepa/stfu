using STFU.Common.Math;

namespace STFU.NPR.Styles;

internal static class NprRandom
{
    public static float SignedFloat(int seed, int channel)
    {
        return HashMath.SignedFloat01(seed, channel);
    }

    public static float Float01(int seed)
    {
        return HashMath.Float01From24Bits(seed);
    }

    public static int Hash(int a, int b)
    {
        return HashMath.Mix(a, b);
    }
}
