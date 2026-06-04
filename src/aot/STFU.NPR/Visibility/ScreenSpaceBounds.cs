namespace STFU.NPR.Visibility;

internal readonly record struct ScreenSpaceBounds(
    float MinX,
    float MinY,
    float MaxX,
    float MaxY)
{
    public float Width => MaxX - MinX;

    public float Height => MaxY - MinY;

    public bool Contains(float x, float y)
    {
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    public static ScreenSpaceBounds Union(ScreenSpaceBounds a, ScreenSpaceBounds b)
    {
        return new ScreenSpaceBounds(
            MathF.Min(a.MinX, b.MinX),
            MathF.Min(a.MinY, b.MinY),
            MathF.Max(a.MaxX, b.MaxX),
            MathF.Max(a.MaxY, b.MaxY));
    }
}
