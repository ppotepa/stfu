namespace STFU.Common.Math;

public static class RasterLineMath
{
    public static int StepCountForLength(float length)
    {
        return NumericMath.AtLeast((int)NumericMath.Ceiling(length), 1);
    }

    public static int PixelRadius(float thickness)
    {
        return NumericMath.AtLeast((int)NumericMath.Round(thickness * 0.5f), 0);
    }

    public static (int X, int Y) RoundedPointAt(float startX, float startY, float dx, float dy, float t)
    {
        return (
            (int)NumericMath.Round(startX + dx * t),
            (int)NumericMath.Round(startY + dy * t));
    }

    public static bool IsInside(int x, int y, int width, int height)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public static int LinearIndex(int x, int y, int width)
    {
        return PixelMemoryMath.Bgra32LinearIndex(x, y, width);
    }
}
