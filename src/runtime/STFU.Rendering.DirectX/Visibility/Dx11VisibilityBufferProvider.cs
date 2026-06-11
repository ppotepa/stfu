using STFU.Rendering.Abstractions.Visibility;
using STFU.Rendering.DirectX.Device;

namespace STFU.Rendering.DirectX.Visibility;

public sealed class Dx11VisibilityBufferProvider : IVisibilityBufferProvider, IDisposable
{
    private readonly DirectXDevice _device;

    public Dx11VisibilityBufferProvider(DirectXDevice device)
    {
        _device = device;
    }

    public bool IsAvailable => !_device.IsDisposed;

    public VisibilityBufferResult BuildVisibility(VisibilityBufferRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new VisibilityBufferResult(
            UsedGpu: false,
            UsedFallback: true,
            request.Width,
            request.Height,
            0,
            Array.Empty<int>());
    }

    public void Dispose()
    {
    }
}
