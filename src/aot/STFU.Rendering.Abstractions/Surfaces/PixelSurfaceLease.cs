namespace STFU.Rendering.Abstractions.Surfaces;

public sealed class PixelSurfaceLease : IDisposable
{
    private readonly Action<PixelSurface>? _return;
    private bool _disposed;

    public PixelSurfaceLease(PixelSurface surface, Action<PixelSurface>? @return)
    {
        Surface = surface;
        _return = @return;
    }

    public PixelSurface Surface { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _return?.Invoke(Surface);
    }
}
