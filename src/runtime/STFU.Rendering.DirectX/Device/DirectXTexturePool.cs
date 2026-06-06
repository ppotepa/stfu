using STFU.Common.Math;
using STFU.Rendering.Abstractions.Gpu;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXTexturePool : IDisposable
{
    private const string DirectXBackendId = "directx-d3d11";
    private const int MaxRetainedPerKey = 4;
    private readonly DirectXDevice _device;
    private readonly object _gate = new();
    private readonly Dictionary<TexturePoolKey, Stack<GpuTextureHandle>> _available = new();
    private readonly int _maxRetained;
    private int _retainedCount;
    private long _createdCount;
    private long _reusedCount;
    private long _disposedCount;
    private bool _disposed;

    public DirectXTexturePool(DirectXDevice device, int maxRetained = 12)
    {
        _device = device;
        _maxRetained = NumericMath.AtLeast(maxRetained, 1);
    }

    public GpuTextureLease RentRenderTarget(int width, int height, GpuSurfaceFormat format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        width = NumericMath.AtLeast(width, 1);
        height = NumericMath.AtLeast(height, 1);
        var key = new TexturePoolKey(width, height, format);

        lock (_gate)
        {
            if (_available.TryGetValue(key, out var stack))
            {
                while (stack.Count > 0)
                {
                    var cached = stack.Pop();
                    _retainedCount--;
                    if (_device.Resources.TryGetTexture(cached, out _))
                    {
                        _reusedCount++;
                        return new GpuTextureLease(cached, Return);
                    }
                }
            }
        }

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
        Interlocked.Increment(ref _createdCount);
        return handle;
    }

    private void Return(GpuTextureHandle handle)
    {
        if (handle.ResourceId == 0)
        {
            return;
        }

        lock (_gate)
        {
            if (_disposed ||
                !_device.Resources.TryGetTexture(handle, out _) ||
                _retainedCount >= _maxRetained)
            {
                _device.Resources.Unregister(handle);
                _disposedCount++;
                return;
            }

            var key = new TexturePoolKey(handle.Width, handle.Height, handle.Format);
            if (!_available.TryGetValue(key, out var stack))
            {
                stack = new Stack<GpuTextureHandle>();
                _available[key] = stack;
            }

            if (stack.Count >= MaxRetainedPerKey)
            {
                _device.Resources.Unregister(handle);
                _disposedCount++;
                return;
            }

            stack.Push(handle);
            _retainedCount++;
        }
    }

    public void Dispose()
    {
        List<GpuTextureHandle> handles = [];
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var stack in _available.Values)
            {
                while (stack.Count > 0)
                {
                    handles.Add(stack.Pop());
                }
            }

            _available.Clear();
            _retainedCount = 0;
        }

        foreach (var handle in handles)
        {
            _device.Resources.Unregister(handle);
            _disposedCount++;
        }
    }

    public DirectXTexturePoolSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new DirectXTexturePoolSnapshot(
                _retainedCount,
                Interlocked.Read(ref _createdCount),
                Interlocked.Read(ref _reusedCount),
                Interlocked.Read(ref _disposedCount));
        }
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

    private readonly record struct TexturePoolKey(int Width, int Height, GpuSurfaceFormat Format);
}

public sealed record DirectXTexturePoolSnapshot(
    int RetainedCount,
    long CreatedCount,
    long ReusedCount,
    long DisposedCount);
