using STFU.Common.Math;
using STFU.Logging;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXSwapChain : IDisposable
{
    private readonly DirectXDevice _device;
    private IDXGISwapChain1? _swapChain;
    private ID3D11Texture2D? _backBuffer;
    private IntPtr _hwnd;
    private int _width;
    private int _height;
    private bool _disposed;

    public DirectXSwapChain(DirectXDevice device)
    {
        _device = device;
    }

    public bool IsAttached => _swapChain is not null && _hwnd != IntPtr.Zero;

    public IntPtr CurrentHwnd => _hwnd;

    public int Width => _width;

    public int Height => _height;

    public void AttachOrResize(IntPtr hwnd, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        width = NumericMath.AtLeast(width, 1);
        height = NumericMath.AtLeast(height, 1);

        if (_swapChain is null || _hwnd != hwnd)
        {
            Recreate(hwnd, width, height);
            return;
        }

        if (_width == width && _height == height)
        {
            return;
        }

        Resize(width, height);
    }

    public void PresentTexture(DirectXTextureResource source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_swapChain is null || _backBuffer is null)
        {
            throw new InvalidOperationException("DirectX swap chain is not attached.");
        }

        var sourceDesc = source.Texture.Description;
        var backBufferDesc = _backBuffer.Description;
        if (sourceDesc.Width != backBufferDesc.Width ||
            sourceDesc.Height != backBufferDesc.Height ||
            sourceDesc.Format != backBufferDesc.Format)
        {
            throw new InvalidOperationException("Swap chain backbuffer does not match GPU source texture.");
        }

        _device.Context.CopyResource(_backBuffer, source.Texture);
        _swapChain.Present(1, PresentFlags.None).CheckError();
    }

    public void Detach()
    {
        DisposeBackBuffer();
        _swapChain?.Dispose();
        _swapChain = null;
        _hwnd = IntPtr.Zero;
        _width = 0;
        _height = 0;
        StfuLog.Write(StfuLogDomain.DirectX, "swapchain.detached", "DirectX swapchain detached.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Detach();
    }

    private void Recreate(IntPtr hwnd, int width, int height)
    {
        Detach();

        var description = new SwapChainDescription1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.None
        };

        _swapChain = _device.Factory.CreateSwapChainForHwnd(_device.Device, hwnd, description);
        _hwnd = hwnd;
        _width = width;
        _height = height;
        RefreshBackBuffer();
        StfuLog.Write(
            StfuLogDomain.DirectX,
            "swapchain.attached",
            $"hwnd=0x{hwnd.ToInt64():X}",
            properties: new Dictionary<string, object?>
            {
                ["width"] = width,
                ["height"] = height
            });
    }

    private void Resize(int width, int height)
    {
        if (_swapChain is null)
        {
            return;
        }

        DisposeBackBuffer();
        _swapChain.ResizeBuffers(0, (uint)width, (uint)height, Format.B8G8R8A8_UNorm, SwapChainFlags.None).CheckError();
        _width = width;
        _height = height;
        RefreshBackBuffer();
        StfuLog.Write(
            StfuLogDomain.DirectX,
            "swapchain.resized",
            $"{width}x{height}",
            properties: new Dictionary<string, object?>
            {
                ["width"] = width,
                ["height"] = height
            });
    }

    private void RefreshBackBuffer()
    {
        DisposeBackBuffer();
        _backBuffer = _swapChain?.GetBuffer<ID3D11Texture2D>(0);
    }

    private void DisposeBackBuffer()
    {
        _backBuffer?.Dispose();
        _backBuffer = null;
    }
}
