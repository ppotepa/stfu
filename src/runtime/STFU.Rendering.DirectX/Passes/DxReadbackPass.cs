using System.Runtime.InteropServices;
using STFU.Rendering.Abstractions.Gpu;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.DirectX.Device;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxReadbackPass
{
    private readonly DirectXDevice _device;
    private readonly PixelSurfacePool _surfacePool;

    public DxReadbackPass(DirectXDevice device, PixelSurfacePool surfacePool)
    {
        _device = device;
        _surfacePool = surfacePool;
    }

    public PixelSurfaceLease ReadToPixelSurface(GpuTextureHandle handle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_device.Resources.TryGetTexture(handle, out var resource))
        {
            throw new InvalidOperationException("GPU texture handle could not be resolved for readback.");
        }

        var desc = resource.Texture.Description;
        var stagingDesc = new Texture2DDescription
        {
            Width = desc.Width,
            Height = desc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = desc.Format,
            SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        using var staging = _device.Device.CreateTexture2D(stagingDesc);
        _device.Context.CopyResource(staging, resource.Texture);
        var mapped = _device.Context.Map(staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            var lease = _surfacePool.Rent(handle.Width, handle.Height, PixelSurfaceFormat.Bgra8888Premultiplied);
            var surface = lease.Surface;
            var rowBytes = Math.Min(surface.Stride, (int)mapped.RowPitch);

            for (var y = 0; y < surface.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch);
                Marshal.Copy(source, surface.Pixels, y * surface.Stride, rowBytes);
            }

            return lease;
        }
        finally
        {
            _device.Context.Unmap(staging, 0);
        }
    }
}
