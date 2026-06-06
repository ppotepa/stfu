namespace STFU.Common.Math;

public static class NoiseMath
{
    private const int NoiseSize = 65536;
    private static readonly double[] Table = Build();

    public static double Noise01(double x, int seed, bool fast)
    {
        if (fast)
        {
            var xi = (int)NumericMath.Floor(x * 4096d);
            var si = (int)NumericMath.Floor(seed * 131.542391d);
            unchecked
            {
                var hash = (uint)((xi ^ si ^ unchecked((int)0x9e3779b9)) * 1103515245 + 12345);
                hash ^= hash >> 16;
                return Table[hash & (NoiseSize - 1)];
            }
        }

        return FractionalSineNoise(x, seed);
    }

    private static double[] Build()
    {
        var values = new double[NoiseSize];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = FractionalSineNoise(i, 1);
        }

        return values;
    }

    private static double FractionalSineNoise(double x, int seed)
    {
        var value = NumericMath.Sin(x * 12.9898d + seed * 78.233d) * 43758.5453d;
        return value - NumericMath.Floor(value);
    }
}
