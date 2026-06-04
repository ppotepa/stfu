namespace STFU.NPR.Pipeline.Default.Steps;

internal static class DefaultNoise
{
    private const int NoiseSize = 65536;
    private static readonly double[] Table = Build();

    public static double Noise01(double x, int seed, bool fast)
    {
        if (fast)
        {
            var xi = (int)Math.Floor(x * 4096d);
            var si = (int)Math.Floor(seed * 131.542391d);
            unchecked
            {
                var hash = (uint)((xi ^ si ^ unchecked((int)0x9e3779b9)) * 1103515245 + 12345);
                hash ^= hash >> 16;
                return Table[hash & (NoiseSize - 1)];
            }
        }

        var value = Math.Sin(x * 12.9898d + seed * 78.233d) * 43758.5453d;
        return value - Math.Floor(value);
    }

    private static double[] Build()
    {
        var values = new double[NoiseSize];
        for (var i = 0; i < values.Length; i++)
        {
            var value = Math.Sin(i * 12.9898d + 78.233d) * 43758.5453d;
            values[i] = value - Math.Floor(value);
        }

        return values;
    }
}
