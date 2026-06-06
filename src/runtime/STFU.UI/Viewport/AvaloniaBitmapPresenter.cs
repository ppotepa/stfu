using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using STFU.Common.Math;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;

namespace STFU.UI;

internal sealed class AvaloniaBitmapPresenter
{
    private WriteableBitmap? _bitmap;
    private int _width;
    private int _height;

    public bool HasFrame => _bitmap is not null;

    public void Present(NprRenderResult result)
    {
        if (result.OutputKind != NprRenderOutputKind.PixelSurface || result.PixelSurfaceLease is null)
        {
            return;
        }

        var surface = result.PixelSurfaceLease.Surface;
        EnsureBitmap(surface.Width, surface.Height);

        if (_bitmap is null)
        {
            return;
        }

        using var framebuffer = _bitmap.Lock();
        CopySurfaceToFramebuffer(surface, framebuffer);
    }

    public void Draw(DrawingContext context, Rect bounds, Color fallbackColor)
    {
        if (_bitmap is null)
        {
            context.FillRectangle(new SolidColorBrush(fallbackColor), bounds);
            return;
        }

        context.DrawImage(
            _bitmap,
            new Rect(0, 0, _width, _height),
            bounds);
    }

    private void EnsureBitmap(int width, int height)
    {
        if (_bitmap is not null && _width == width && _height == height)
        {
            return;
        }

        _width = NumericMath.AtLeast(width, 1);
        _height = NumericMath.AtLeast(height, 1);
        _bitmap = new WriteableBitmap(
            new PixelSize(_width, _height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
    }

    private static void CopySurfaceToFramebuffer(PixelSurface surface, ILockedFramebuffer framebuffer)
    {
        if (surface.Format != PixelSurfaceFormat.Bgra8888Premultiplied)
        {
            throw new NotSupportedException($"Unsupported PixelSurface format: {surface.Format}");
        }

        if (framebuffer.RowBytes == surface.Stride)
        {
            Marshal.Copy(surface.Pixels, 0, framebuffer.Address, surface.ByteLength);
            return;
        }

        for (var y = 0; y < surface.Height; y++)
        {
            var sourceOffset = y * surface.Stride;
            var target = IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes);
            Marshal.Copy(surface.Pixels, sourceOffset, target, surface.Stride);
        }
    }
}
