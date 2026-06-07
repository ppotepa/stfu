using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.UI.Bridge.Session;
using STFU.Logging;

namespace STFU.UI;

internal enum DirectXPresentAvailability
{
    Ready,
    NotWindows,
    DeviceUnavailable,
    SwapChainUnavailable,
    DeviceDisposed,
    NotAttached,
    NotGpuTexture,
    NullGpuTextureLease,
    SizeMismatch
}

internal sealed class DirectXViewportPresenter : IViewportPresenter, IDisposable
{
    private readonly DirectXDevice? _device;
    private readonly DirectXSwapChain? _swapChain;
    private bool _disposed;
    private long _sizingCheckCounter;

    public DirectXViewportPresenter(UiEngineSession session)
    {
        _device = session.Engine.Registry.TryGet<DirectXDevice>(out var device) ? device : null;
        if (_device is not null)
        {
            _swapChain = new DirectXSwapChain(_device);
        }
    }

    public bool IsAvailable => OperatingSystem.IsWindows() && _device is not null && _swapChain is not null && !_device.IsDisposed;

    public ViewportPresentationKind Kind => ViewportPresentationKind.DirectGpu;

    public bool IsAttached => _swapChain?.IsAttached == true;

    public int SwapChainWidth => _swapChain?.Width ?? 0;

    public int SwapChainHeight => _swapChain?.Height ?? 0;

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

    public DirectXPresentAvailability GetAvailability(NprRenderResult result)
    {
        if (!OperatingSystem.IsWindows())
        {
            return DirectXPresentAvailability.NotWindows;
        }

        if (_device is null)
        {
            return DirectXPresentAvailability.DeviceUnavailable;
        }

        if (_swapChain is null)
        {
            return DirectXPresentAvailability.SwapChainUnavailable;
        }

        if (_device.IsDisposed)
        {
            return DirectXPresentAvailability.DeviceDisposed;
        }

        if (!IsAttached)
        {
            return DirectXPresentAvailability.NotAttached;
        }

        if (result.OutputKind != NprRenderOutputKind.GpuTexture)
        {
            return DirectXPresentAvailability.NotGpuTexture;
        }

        if (result.GpuTextureLease is null)
        {
            return DirectXPresentAvailability.NullGpuTextureLease;
        }

        return DirectXPresentAvailability.Ready;
    }

    public bool CanPresent(NprRenderResult result)
    {
        return GetAvailability(result) == DirectXPresentAvailability.Ready;
    }

    public bool Present(NprRenderResult result)
    {
        return TryPresent(result, out DirectXPresentAvailability _);
    }

    public bool TryPresent(NprRenderResult result, out string availability)
    {
        var presented = TryPresent(result, out DirectXPresentAvailability directAvailability);
        availability = directAvailability.ToString();
        return presented;
    }

    public bool TryPresent(
        NprRenderResult result,
        out DirectXPresentAvailability availability)
    {
        availability = GetAvailability(result);
        if (availability != DirectXPresentAvailability.Ready)
        {
            return false;
        }

        if (result.GpuTextureLease is null)
        {
            availability = DirectXPresentAvailability.NullGpuTextureLease;
            return false;
        }

        var swapChain = _swapChain!;
        using var deviceLock = _device!.Lock();
        if (!_device.Resources.TryGetTexture(result.GpuTextureLease.Texture, out var source))
        {
            availability = DirectXPresentAvailability.NullGpuTextureLease;
            StfuLog.Write(
                StfuLogDomain.Viewport,
                "gpu_present.exception",
                $"DirectX texture handle was invalid for revision {result.Revision}.",
                StfuLogLevel.Warning,
                new Dictionary<string, object?>
                {
                    ["revision"] = result.Revision,
                    ["outputKind"] = result.OutputKind,
                    ["resourceId"] = result.GpuTextureLease.Texture.ResourceId,
                    ["sourceWidth"] = result.GpuTextureLease.Texture.Width,
                    ["sourceHeight"] = result.GpuTextureLease.Texture.Height,
                    ["swapChainWidth"] = swapChain.Width,
                    ["swapChainHeight"] = swapChain.Height,
                    ["availability"] = availability
                });
            return false;
        }

        if (swapChain.Width != source.Handle.Width || swapChain.Height != source.Handle.Height)
        {
            availability = DirectXPresentAvailability.SizeMismatch;
            return false;
        }

        if (_sizingCheckCounter++ % 60 == 0)
        {
            StfuLog.Write(
                StfuLogDomain.Viewport,
                "direct_present.sizing_check",
                $"source={source.Handle.Width}x{source.Handle.Height} " +
                $"swapchain={swapChain.Width}x{swapChain.Height}",
                StfuLogLevel.Debug,
                new Dictionary<string, object?>
                {
                    ["sourceWidth"] = source.Handle.Width,
                    ["sourceHeight"] = source.Handle.Height,
                    ["swapChainWidth"] = swapChain.Width,
                    ["swapChainHeight"] = swapChain.Height,
                    ["isAttached"] = IsAttached
                });
        }

        try
        {
            swapChain.PresentTexture(source);
            availability = DirectXPresentAvailability.Ready;
            return true;
        }
        catch (Exception exception)
        {
            availability = DirectXPresentAvailability.DeviceUnavailable;
            StfuLog.Write(
                StfuLogDomain.Viewport,
                "gpu_present.exception",
                $"DirectX present failed: {exception.Message}",
                StfuLogLevel.Warning,
                new Dictionary<string, object?>
                {
                    ["revision"] = result.Revision,
                    ["outputKind"] = result.OutputKind,
                    ["sourceWidth"] = source.Handle.Width,
                    ["sourceHeight"] = source.Handle.Height,
                    ["swapChainWidth"] = swapChain.Width,
                    ["swapChainHeight"] = swapChain.Height,
                    ["availability"] = availability
                },
                exception);
            return false;
        }
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
