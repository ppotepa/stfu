using STFU.Common.Memory;

namespace STFU.Common.Math;

public static class PixelMemoryMath
{
    public const int Bgra32BytesPerPixel = PixelBufferMemory.Bgra32BytesPerPixel;

    public static bool IsLittleEndian => PixelBufferMemory.IsLittleEndian;

    public static bool CanFillContiguousBgra32AsPackedUInt32(int width, int stride)
    {
        return PixelBufferMemory.CanFillContiguousBgra32AsPackedUInt32(width, stride);
    }

    public static bool CanFillBgra32RowAsPackedUInt32()
    {
        return PixelBufferMemory.CanFillBgra32RowAsPackedUInt32();
    }

    public static int Bgra32ByteOffset(int x)
    {
        return PixelBufferMemory.Bgra32ByteOffset(x);
    }

    public static int Bgra32LinearIndex(int x, int y, int width)
    {
        return PixelBufferMemory.Bgra32LinearIndex(x, y, width);
    }
}
