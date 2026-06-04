using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.UI.Bridge.Session;

namespace STFU.UI;

internal sealed class DirectXViewportPresenter : IDisposable
{
    private readonly DirectXDevice? _device;
    private readonly DirectXSwapChain? _swapChain;
    private bool _disposed;

    public DirectXViewportPresenter(UiEngineSession session)
    {
        _device = session.Engine.Registry.TryGet<DirectXDevice>(out var device) ? device : null;
        if (_device is not null)
        {
            _swapChain = new DirectXSwapChain(_device);
        }
    }

    public bool IsAvailable => OperatingSystem.IsWindows() && _device is not null && _swapChain is not null && !_device.IsDisposed;

    public bool IsAttached => _swapChain?.IsAttached == true;

    public void Attach(IntPtr hwnd, int width, int height)
    {
        if (!IsAvailable)
        {
            return;
        }

        using var deviceLock = _device!.Lock();
        _swapChain!.AttachOrResize(hwnd, width, height);
    }

    public void Resize(int width, int height)
    {
        if (!IsAvailable || !IsAttached)
        {
            return;
        }

        using var deviceLock = _device!.Lock();
        _swapChain!.AttachOrResize(_swapChain.CurrentHwnd, width, height);
    }

    public bool CanPresent(NprRenderResult result)
    {
        return IsAvailable &&
            IsAttached &&
            result.OutputKind == NprRenderOutputKind.GpuTexture &&
            result.GpuTextureLease is not null;
    }

    public void Present(NprRenderResult result)
    {
        if (!CanPresent(result) || result.GpuTextureLease is null)
        {
            return;
        }

        using var deviceLock = _device!.Lock();
        if (!_device.Resources.TryGetTexture(result.GpuTextureLease.Texture, out var source))
        {
            throw new InvalidOperationException("GPU texture handle could not be resolved for direct presentation.");
        }

        _swapChain!.AttachOrResize(_swapChain.CurrentHwnd, source.Handle.Width, source.Handle.Height);
        _swapChain!.PresentTexture(source);
    }

    public void Detach()
    {
        if (_swapChain is null)
        {
            return;
        }

        using var deviceLock = _device!.Lock();
        _swapChain.Detach();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_swapChain is null || _device is null || _device.IsDisposed)
        {
            _swapChain?.Dispose();
            return;
        }

        using var deviceLock = _device.Lock();
        _swapChain.Dispose();
    }

}
