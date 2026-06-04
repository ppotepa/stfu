using STFU.Rendering.Abstractions.Gpu;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXRenderTarget : IDisposable
{
    private bool _disposed;

    public DirectXRenderTarget(GpuTextureLease lease, DirectXTextureResource resource)
    {
        Lease = lease;
        Resource = resource;
    }

    public GpuTextureLease Lease { get; }

    public DirectXTextureResource Resource { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Lease.Dispose();
    }
}
