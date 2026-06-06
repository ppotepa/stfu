namespace STFU.Common.Math;

public static class SizeMath
{
    public static int AtLeastPixels(int value, int minimum) => RasterMath.AtLeastPixels(value, minimum);

    public static int PixelCount(int width, int height) => RasterMath.PixelCount(width, height);

    public static int CeilingDivide(int value, int divisor) => NumericMath.CeilingDivide(value, divisor);

    public static int ScaleIndex(int targetIndex, int targetCount, int sourceCount)
    {
        return NumericMath.ScaleIndex(targetIndex, targetCount, sourceCount);
    }

    public static double ToMegabytes(long bytes, int digits = 2)
    {
        return BufferSizingMath.ToMegabytes(bytes, digits);
    }
}
