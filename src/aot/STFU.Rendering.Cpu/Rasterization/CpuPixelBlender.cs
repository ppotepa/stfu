using STFU.Common.Math;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;
using System.Runtime.InteropServices;

namespace STFU.Rendering.Cpu.Rasterization;

public static class CpuPixelBlender
{
    public static void Clear(PixelSurface surface, StrokeColor color, byte alpha = 255)
    {
        var r = Premultiply(color.R, alpha);
        var g = Premultiply(color.G, alpha);
        var b = Premultiply(color.B, alpha);
        var packed = (uint)(b | (g << 8) | (r << 16) | (alpha << 24));

        if (BitConverter.IsLittleEndian && surface.Stride == surface.Width * 4)
        {
            MemoryMarshal.Cast<byte, uint>(surface.Span).Fill(packed);
            return;
        }

        for (var y = 0; y < surface.Height; y++)
        {
            var row = surface.Span.Slice(y * surface.Stride, surface.Width * 4);
            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.Cast<byte, uint>(row).Fill(packed);
                continue;
            }

            for (var x = 0; x < surface.Width; x++)
            {
                var i = x * 4;
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
        var srcB = Premultiply(color.B, srcA);
        var srcG = Premultiply(color.G, srcA);
        var srcR = Premultiply(color.R, srcA);
        var dstIndex = y * surface.Stride + x * 4;
        var pixels = surface.Pixels;
        var invA = 255 - srcA;

        pixels[dstIndex] = NumericMath.SaturatingAddByte(srcB, pixels[dstIndex] * invA / 255);
        pixels[dstIndex + 1] = NumericMath.SaturatingAddByte(srcG, pixels[dstIndex + 1] * invA / 255);
        pixels[dstIndex + 2] = NumericMath.SaturatingAddByte(srcR, pixels[dstIndex + 2] * invA / 255);
        pixels[dstIndex + 3] = NumericMath.SaturatingAddByte(srcA, pixels[dstIndex + 3] * invA / 255);
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

        var dstIndex = y * surface.Stride + x * 4;
        var pixels = surface.Pixels;
        var invA = 255 - alpha;

        pixels[dstIndex] = NumericMath.SaturatingAddByte(premulB, pixels[dstIndex] * invA / 255);
        pixels[dstIndex + 1] = NumericMath.SaturatingAddByte(premulG, pixels[dstIndex + 1] * invA / 255);
        pixels[dstIndex + 2] = NumericMath.SaturatingAddByte(premulR, pixels[dstIndex + 2] * invA / 255);
        pixels[dstIndex + 3] = NumericMath.SaturatingAddByte(alpha, pixels[dstIndex + 3] * invA / 255);
    }

    public static byte Premultiply(byte color, byte alpha)
    {
        return (byte)(color * alpha / 255);
    }
}
