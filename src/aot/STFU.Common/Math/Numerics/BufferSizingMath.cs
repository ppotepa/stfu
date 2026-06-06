namespace STFU.Common.Math;

public static class BufferSizingMath
{
    public static double ToMegabytes(long bytes, int digits = 2)
    {
        return NumericMath.Round(bytes / 1024.0 / 1024.0, digits);
    }
}
