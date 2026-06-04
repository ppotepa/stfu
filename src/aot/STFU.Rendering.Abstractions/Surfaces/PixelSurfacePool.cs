using System.Collections.Concurrent;

namespace STFU.Rendering.Abstractions.Surfaces;

public sealed class PixelSurfacePool
{
    private readonly ConcurrentBag<PixelSurface> _surfaces = new();
    private readonly int _maxRetainedSurfaces;
    private int _retainedCount;

    public PixelSurfacePool(int maxRetainedSurfaces = 4)
    {
        _maxRetainedSurfaces = Math.Max(1, maxRetainedSurfaces);
    }

    public PixelSurfaceLease Rent(int width, int height, PixelSurfaceFormat format = PixelSurfaceFormat.Bgra8888Premultiplied)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var stride = checked(width * 4);
        var byteLength = checked(stride * height);

        while (_surfaces.TryTake(out var candidate))
        {
            Interlocked.Decrement(ref _retainedCount);
            if (candidate.Width == width &&
                candidate.Height == height &&
                candidate.Stride == stride &&
                candidate.Format == format &&
                candidate.Pixels.Length >= byteLength)
            {
                candidate.Span.Clear();
                return new PixelSurfaceLease(candidate, Return);
            }
        }

        var surface = new PixelSurface(width, height, stride, format, new byte[byteLength]);
        return new PixelSurfaceLease(surface, Return);
    }

    private void Return(PixelSurface surface)
    {
        if (Interlocked.Increment(ref _retainedCount) > _maxRetainedSurfaces)
        {
            Interlocked.Decrement(ref _retainedCount);
            return;
        }

        _surfaces.Add(surface);
    }
}
