namespace STFU.Common.Math;

public static class GpuPackingMath
{
    public static float InverseDimension(int value, int minimum = 1)
    {
        return NumericMath.InverseAtLeast(value, minimum);
    }

    public static int ClampResourceSize(int value, int minimum = 1)
    {
        return NumericMath.AtLeast(value, minimum);
    }

    public static double ToMegabytes(long bytes, int digits = 2)
    {
        return BufferSizingMath.ToMegabytes(bytes, digits);
    }
}
