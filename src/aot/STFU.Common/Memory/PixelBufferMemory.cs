namespace STFU.Common.Memory;

/// <summary>
/// Pixel memory layout helpers for tightly or stride-backed BGRA32 buffers.
/// </summary>
public static class PixelBufferMemory
{
    public const int Bgra32BytesPerPixel = 4;

    public static bool IsLittleEndian => BitConverter.IsLittleEndian;

    public static bool CanFillContiguousBgra32AsPackedUInt32(int width, int stride)
    {
        return IsLittleEndian && stride == checked(width * Bgra32BytesPerPixel);
    }

    public static bool CanFillBgra32RowAsPackedUInt32()
    {
        return IsLittleEndian;
    }

    public static int Bgra32ByteOffset(int x)
    {
        return checked(x * Bgra32BytesPerPixel);
    }

    public static int Bgra32LinearIndex(int x, int y, int width)
    {
        return checked((y * width) + x);
    }
}
