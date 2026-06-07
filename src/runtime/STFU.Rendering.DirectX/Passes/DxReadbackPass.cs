using System.Runtime.InteropServices;
using STFU.Common.Math;
using STFU.Rendering.Abstractions.Gpu;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.DirectX.Device;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxReadbackPass
{
    private readonly DirectXDevice _device;
    private readonly PixelSurfacePool _surfacePool;

    public DxReadbackCounters Counters { get; } = new();

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

        using (_device.Lock())
        {
            var desc = resource.Texture.Description;
            using var stagingLease = _device.ReadbackTexturePool.Rent((int)desc.Width, (int)desc.Height, desc.Format);
            var staging = stagingLease.Texture;
            _device.Context.CopyResource(staging, resource.Texture);
            var mapped = _device.Context.Map(staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
            try
            {
                var lease = _surfacePool.Rent(handle.Width, handle.Height, PixelSurfaceFormat.Bgra8888Premultiplied);
                var surface = lease.Surface;
                var rowBytes = NumericMath.AtMost(surface.Stride, (int)mapped.RowPitch);

                for (var y = 0; y < surface.Height; y++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var source = IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch);
                    Marshal.Copy(source, surface.Pixels, y * surface.Stride, rowBytes);
                }

                Counters.RecordReadback(surface.Height, rowBytes);
                _ = Counters.Readbacks;
                return lease;
            }
            finally
            {
                _device.Context.Unmap(staging, 0);
            }
        }
    }
}
