using STFU.Common.Math;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;
using System.Runtime.InteropServices;

namespace STFU.Rendering.Cpu.Rasterization;

public static class CpuPixelBlender
{
    public static void Clear(PixelSurface surface, StrokeColor color, byte alpha = 255)
    {
        var r = ColorBlendMath.Premultiply(color.R, alpha);
        var g = ColorBlendMath.Premultiply(color.G, alpha);
        var b = ColorBlendMath.Premultiply(color.B, alpha);
        var packed = ColorBlendMath.PackBgra32Premultiplied(color.R, color.G, color.B, alpha);

        if (PixelMemoryMath.CanFillContiguousBgra32AsPackedUInt32(surface.Width, surface.Stride))
        {
            MemoryMarshal.Cast<byte, uint>(surface.Span).Fill(packed);
            return;
        }

        for (var y = 0; y < surface.Height; y++)
        {
            var row = surface.Span.Slice(y * surface.Stride, surface.Width * 4);
            if (PixelMemoryMath.CanFillBgra32RowAsPackedUInt32())
            {
                MemoryMarshal.Cast<byte, uint>(row).Fill(packed);
                continue;
            }

            for (var x = 0; x < surface.Width; x++)
            {
                var i = PixelMemoryMath.Bgra32ByteOffset(x);
                row[i] = b;
                row[i + 1] = g;
                row[i + 2] = r;
                row[i + 3] = alpha;
            }
        }
    }

    public static void BlendSourceOver(
        PixelSurface surface,
        int x,
        int y,
        StrokeColor color,
        float alpha)
    {
        if ((uint)x >= (uint)surface.Width || (uint)y >= (uint)surface.Height)
        {
            return;
        }

        alpha = NumericMath.Clamp01(alpha);
        if (alpha <= 0f)
        {
            return;
        }

        var srcA = NumericMath.UnitToByte(alpha);
        var srcB = ColorBlendMath.Premultiply(color.B, srcA);
        var srcG = ColorBlendMath.Premultiply(color.G, srcA);
        var srcR = ColorBlendMath.Premultiply(color.R, srcA);
        var dstIndex = y * surface.Stride + PixelMemoryMath.Bgra32ByteOffset(x);
        var pixels = surface.Pixels;
        pixels[dstIndex] = ColorBlendMath.SourceOverChannel(srcB, pixels[dstIndex], srcA);
        pixels[dstIndex + 1] = ColorBlendMath.SourceOverChannel(srcG, pixels[dstIndex + 1], srcA);
        pixels[dstIndex + 2] = ColorBlendMath.SourceOverChannel(srcR, pixels[dstIndex + 2], srcA);
        pixels[dstIndex + 3] = ColorBlendMath.SourceOverChannel(srcA, pixels[dstIndex + 3], srcA);
    }

    public static void BlendSourceOverBgraPremultiplied(
        PixelSurface surface,
        int x,
        int y,
        byte premulB,
        byte premulG,
        byte premulR,
        byte alpha)
    {
        if ((uint)x >= (uint)surface.Width || (uint)y >= (uint)surface.Height || alpha == 0)
        {
            return;
        }

        var dstIndex = y * surface.Stride + PixelMemoryMath.Bgra32ByteOffset(x);
        var pixels = surface.Pixels;
        pixels[dstIndex] = ColorBlendMath.SourceOverChannel(premulB, pixels[dstIndex], alpha);
        pixels[dstIndex + 1] = ColorBlendMath.SourceOverChannel(premulG, pixels[dstIndex + 1], alpha);
        pixels[dstIndex + 2] = ColorBlendMath.SourceOverChannel(premulR, pixels[dstIndex + 2], alpha);
        pixels[dstIndex + 3] = ColorBlendMath.SourceOverChannel(alpha, pixels[dstIndex + 3], alpha);
    }

}
