namespace STFU.NPR.Styles;

internal static class NprRandom
{
    public static float SignedFloat(int seed, int channel)
    {
        return Float01(Hash(seed, channel)) * 2f - 1f;
    }

    public static float Float01(int seed)
    {
        return (Hash(seed, 0) & 0x00FFFFFF) / 16777215f;
    }

    public static int Hash(int a, int b)
    {
        unchecked
        {
            var value = (uint)a;
            value ^= (uint)b + 0x9E3779B9u + (value << 6) + (value >> 2);
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return (int)value;
        }
    }
}
