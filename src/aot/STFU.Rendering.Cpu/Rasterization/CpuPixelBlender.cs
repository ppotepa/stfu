using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public static class CpuPixelBlender
{
    public static void Clear(PixelSurface surface, StrokeColor color, byte alpha = 255)
    {
        var r = Premultiply(color.R, alpha);
        var g = Premultiply(color.G, alpha);
        var b = Premultiply(color.B, alpha);
        var span = surface.Span;

        for (var y = 0; y < surface.Height; y++)
        {
            var row = y * surface.Stride;
            for (var x = 0; x < surface.Width; x++)
            {
                var i = row + x * 4;
                span[i] = b;
                span[i + 1] = g;
                span[i + 2] = r;
                span[i + 3] = alpha;
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

        alpha = Math.Clamp(alpha, 0f, 1f);
        if (alpha <= 0f)
        {
            return;
        }

        var srcA = (byte)Math.Clamp((int)MathF.Round(alpha * 255f), 0, 255);
        var srcB = Premultiply(color.B, srcA);
        var srcG = Premultiply(color.G, srcA);
        var srcR = Premultiply(color.R, srcA);
        var dstIndex = y * surface.Stride + x * 4;
        var pixels = surface.Pixels;
        var invA = 255 - srcA;

        pixels[dstIndex] = (byte)Math.Min(255, srcB + pixels[dstIndex] * invA / 255);
        pixels[dstIndex + 1] = (byte)Math.Min(255, srcG + pixels[dstIndex + 1] * invA / 255);
        pixels[dstIndex + 2] = (byte)Math.Min(255, srcR + pixels[dstIndex + 2] * invA / 255);
        pixels[dstIndex + 3] = (byte)Math.Min(255, srcA + pixels[dstIndex + 3] * invA / 255);
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

        pixels[dstIndex] = (byte)Math.Min(255, premulB + pixels[dstIndex] * invA / 255);
        pixels[dstIndex + 1] = (byte)Math.Min(255, premulG + pixels[dstIndex + 1] * invA / 255);
        pixels[dstIndex + 2] = (byte)Math.Min(255, premulR + pixels[dstIndex + 2] * invA / 255);
        pixels[dstIndex + 3] = (byte)Math.Min(255, alpha + pixels[dstIndex + 3] * invA / 255);
    }

    public static byte Premultiply(byte color, byte alpha)
    {
        return (byte)(color * alpha / 255);
    }
}
