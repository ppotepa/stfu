using STFU.Rendering.Abstractions.Gpu;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXTexturePool : IDisposable
{
    private const string DirectXBackendId = "directx-d3d11";
    private readonly DirectXDevice _device;
    private bool _disposed;

    public DirectXTexturePool(DirectXDevice device, int maxRetained = 4)
    {
        _device = device;
    }

    public GpuTextureLease RentRenderTarget(int width, int height, GpuSurfaceFormat format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var handle = CreateRenderTarget(width, height, format);
        return new GpuTextureLease(handle, Return);
    }

    private GpuTextureHandle CreateRenderTarget(int width, int height, GpuSurfaceFormat format)
    {
        var dxgiFormat = ToDxgiFormat(format);
        var id = DirectXResourceId.Next();
        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = dxgiFormat,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        var texture = _device.Device.CreateTexture2D(desc);
        var renderTargetView = _device.Device.CreateRenderTargetView(texture);
        var shaderResourceView = _device.Device.CreateShaderResourceView(texture);
        var handle = new GpuTextureHandle(
            DirectXBackendId,
            id,
            width,
            height,
            format,
            GpuTextureUsage.RenderTarget | GpuTextureUsage.ShaderResource | GpuTextureUsage.TransferSource);

        _device.Resources.Register(new DirectXTextureResource(handle, texture, renderTargetView, shaderResourceView));
        return handle;
    }

    private void Return(GpuTextureHandle handle)
    {
        _device.Resources.Unregister(handle);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    public static Format ToDxgiFormat(GpuSurfaceFormat format)
    {
        return format switch
        {
            GpuSurfaceFormat.Bgra8888Unorm => Format.B8G8R8A8_UNorm,
            GpuSurfaceFormat.Bgra8888UnormSrgb => Format.B8G8R8A8_UNorm_SRgb,
            GpuSurfaceFormat.Rgba8888Unorm => Format.R8G8B8A8_UNorm,
            GpuSurfaceFormat.Rgba8888UnormSrgb => Format.R8G8B8A8_UNorm_SRgb,
            _ => Format.B8G8R8A8_UNorm
        };
    }
}
