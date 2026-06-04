namespace STFU.Rendering.Abstractions.Gpu;

public sealed class GpuTextureLease : IDisposable
{
    private readonly Action<GpuTextureHandle>? _return;
    private bool _disposed;

    public GpuTextureLease(GpuTextureHandle texture, Action<GpuTextureHandle>? @return)
    {
        Texture = texture;
        _return = @return;
    }

    public GpuTextureHandle Texture { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _return?.Invoke(Texture);
    }
}
