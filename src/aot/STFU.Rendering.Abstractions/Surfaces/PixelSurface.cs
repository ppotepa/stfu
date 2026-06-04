namespace STFU.Rendering.Abstractions.Surfaces;

public sealed class PixelSurface
{
    public PixelSurface(
        int width,
        int height,
        int stride,
        PixelSurfaceFormat format,
        byte[] pixels)
    {
        Width = width;
        Height = height;
        Stride = stride;
        Format = format;
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public PixelSurfaceFormat Format { get; }

    public byte[] Pixels { get; }

    public int ByteLength => Stride * Height;

    public Span<byte> Span => Pixels.AsSpan(0, ByteLength);

    public Memory<byte> Memory => Pixels.AsMemory(0, ByteLength);
}
