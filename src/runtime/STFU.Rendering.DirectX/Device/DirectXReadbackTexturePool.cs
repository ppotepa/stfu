using STFU.Common.Math;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXReadbackTexturePool : IDisposable
{
    private readonly DirectXDevice _device;
    private readonly object _gate = new();
    private readonly Dictionary<ReadbackTextureKey, Stack<ID3D11Texture2D>> _available = new();
    private readonly int _maxRetained;
    private int _retainedCount;
    private long _createdCount;
    private long _reusedCount;
    private long _disposedCount;
    private bool _disposed;

    public DirectXReadbackTexturePool(DirectXDevice device, int maxRetained = 2)
    {
        _device = device;
        _maxRetained = NumericMath.AtLeast(maxRetained, 1);
    }

    public DirectXReadbackTextureLease Rent(int width, int height, Format format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        width = NumericMath.AtLeast(width, 1);
        height = NumericMath.AtLeast(height, 1);
        var key = new ReadbackTextureKey(width, height, format);

        lock (_gate)
        {
            if (_available.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                _retainedCount--;
                _reusedCount++;
                return new DirectXReadbackTextureLease(stack.Pop(), Return);
            }
        }

        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        _createdCount++;
        return new DirectXReadbackTextureLease(_device.Device.CreateTexture2D(desc), Return);
    }

    private void Return(ID3D11Texture2D texture)
    {
        var desc = texture.Description;
        var key = new ReadbackTextureKey((int)desc.Width, (int)desc.Height, desc.Format);

        lock (_gate)
        {
            if (_disposed || _retainedCount >= _maxRetained)
            {
                texture.Dispose();
                _disposedCount++;
                return;
            }

            if (!_available.TryGetValue(key, out var stack))
            {
                stack = new Stack<ID3D11Texture2D>();
                _available[key] = stack;
            }

            stack.Push(texture);
            _retainedCount++;
        }
    }

    public DirectXTexturePoolSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new DirectXTexturePoolSnapshot(_retainedCount, _createdCount, _reusedCount, _disposedCount);
        }
    }

    public void Dispose()
    {
        List<ID3D11Texture2D> textures = [];
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
                    textures.Add(stack.Pop());
                }
            }

            _available.Clear();
            _retainedCount = 0;
        }

        foreach (var texture in textures)
        {
            texture.Dispose();
            _disposedCount++;
        }
    }

    private readonly record struct ReadbackTextureKey(int Width, int Height, Format Format);
}

public sealed class DirectXReadbackTextureLease : IDisposable
{
    private readonly Action<ID3D11Texture2D> _return;
    private bool _disposed;

    public DirectXReadbackTextureLease(ID3D11Texture2D texture, Action<ID3D11Texture2D> @return)
    {
        Texture = texture;
        _return = @return;
    }

    public ID3D11Texture2D Texture { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _return(Texture);
    }
}
