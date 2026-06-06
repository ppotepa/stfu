using STFU.Common.Math;

namespace STFU.Rendering.Abstractions.Surfaces;

public sealed class PixelSurfacePool
{
    private readonly object _gate = new();
    private readonly Dictionary<PixelSurfacePoolKey, Stack<PixelSurface>> _available = [];
    private readonly int _maxRetainedPerKey;
    private readonly int _maxRetainedTotal;
    private int _retainedTotal;
    private long _createdCount;
    private long _reusedCount;
    private long _returnedCount;
    private long _discardedCount;

    public PixelSurfacePool(int maxRetainedSurfaces = 4, int? maxRetainedPerKey = null)
    {
        _maxRetainedTotal = NumericMath.AtLeast(maxRetainedSurfaces, 1);
        _maxRetainedPerKey = NumericMath.AtLeast(maxRetainedPerKey ?? maxRetainedSurfaces, 1);
    }

    public PixelSurfaceLease Rent(int width, int height, PixelSurfaceFormat format = PixelSurfaceFormat.Bgra8888Premultiplied)
    {
        width = NumericMath.AtLeast(width, 1);
        height = NumericMath.AtLeast(height, 1);
        var stride = checked(width * 4);
        var key = new PixelSurfacePoolKey(width, height, stride, format);

        lock (_gate)
        {
            if (_available.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                _retainedTotal--;
                _reusedCount++;
                return new PixelSurfaceLease(stack.Pop(), Return);
            }
        }

        var byteLength = checked(stride * height);
        var surface = new PixelSurface(
            width,
            height,
            stride,
            format,
            GC.AllocateUninitializedArray<byte>(byteLength));
        _createdCount++;
        return new PixelSurfaceLease(surface, Return);
    }

    public PixelSurfacePoolSnapshot Snapshot()
    {
        lock (_gate)
        {
            long retainedBytes = 0;
            foreach (var entry in _available)
            {
                foreach (var surface in entry.Value)
                {
                    retainedBytes += surface.ByteLength;
                }
            }

            return new PixelSurfacePoolSnapshot(
                CreatedCount: _createdCount,
                ReusedCount: _reusedCount,
                ReturnedCount: _returnedCount,
                DiscardedCount: _discardedCount,
                RetainedCount: _retainedTotal,
                RetainedBytes: retainedBytes);
        }
    }

    private void Return(PixelSurface surface)
    {
        var key = new PixelSurfacePoolKey(surface.Width, surface.Height, surface.Stride, surface.Format);

        lock (_gate)
        {
            _returnedCount++;

            if (_retainedTotal >= _maxRetainedTotal)
            {
                _discardedCount++;
                return;
            }

            if (!_available.TryGetValue(key, out var stack))
            {
                stack = [];
                _available.Add(key, stack);
            }

            if (stack.Count >= _maxRetainedPerKey)
            {
                _discardedCount++;
                return;
            }

            stack.Push(surface);
            _retainedTotal++;
        }
    }

    private readonly record struct PixelSurfacePoolKey(
        int Width,
        int Height,
        int Stride,
        PixelSurfaceFormat Format);
}

public readonly record struct PixelSurfacePoolSnapshot(
    long CreatedCount,
    long ReusedCount,
    long ReturnedCount,
    long DiscardedCount,
    int RetainedCount,
    long RetainedBytes);
