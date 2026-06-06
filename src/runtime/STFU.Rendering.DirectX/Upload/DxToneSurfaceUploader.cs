using STFU.Common.Math;
using STFU.NPR.Rendering;
using STFU.Rendering.DirectX.Device;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace STFU.Rendering.DirectX.Upload;

public sealed class DxToneSurfaceUploader : IDisposable
{
    private readonly DirectXDevice _device;
    private readonly object _gate = new();
    private readonly Dictionary<ToneTextureKey, Stack<DxToneTextureResource>> _available = new();
    private readonly Dictionary<ToneTextureContentKey, ToneTextureCacheEntry> _contentCache = new();
    private readonly LinkedList<ToneTextureContentKey> _contentLru = new();
    private readonly int _maxRetainedPerKey;
    private readonly int _maxCachedContent;
    private bool _disposed;

    public DxToneSurfaceUploader(DirectXDevice device, int maxRetainedPerKey = 8, int maxCachedContent = 32)
    {
        _device = device;
        _maxRetainedPerKey = NumericMath.AtLeast(maxRetainedPerKey, 1);
        _maxCachedContent = NumericMath.AtLeast(maxCachedContent, 1);
    }

    public unsafe DxToneSurfaceUpload? Upload(NprToneSurface2D tone, out bool cacheHit)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cacheHit = false;

        if (tone.Width <= 0 ||
            tone.Height <= 0 ||
            tone.Rgba.Length < tone.Width * tone.Height * 4)
        {
            return null;
        }

        var expectedBytes = tone.Width * tone.Height * 4;
        var contentKey = new ToneTextureContentKey(
            tone.Width,
            tone.Height,
            expectedBytes,
            ComputeHash(tone.Rgba.AsSpan(0, expectedBytes)));

        lock (_gate)
        {
            if (_contentCache.TryGetValue(contentKey, out var entry))
            {
                cacheHit = true;
                Touch(entry);
                return new DxToneSurfaceUpload(tone, entry.Resource, static _ => { });
            }
        }

        DxToneTextureResource resource;
        using (_device.Lock())
        {
            resource = Rent(tone.Width, tone.Height);
            fixed (byte* rgbaPtr = tone.Rgba)
            {
                _device.Context.UpdateSubresource(
                    resource.Texture,
                    0,
                    null,
                    (IntPtr)rgbaPtr,
                    (uint)(tone.Width * 4),
                    0);
            }
        }

        AddToContentCache(contentKey, resource);
        return new DxToneSurfaceUpload(tone, resource, static _ => { });
    }

    public void Dispose()
    {
        List<DxToneTextureResource> resources = [];
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
                    resources.Add(stack.Pop());
                }
            }

            _available.Clear();
            foreach (var entry in _contentCache.Values)
            {
                resources.Add(entry.Resource);
            }

            _contentCache.Clear();
            _contentLru.Clear();
        }

        for (var i = 0; i < resources.Count; i++)
        {
            resources[i].Dispose();
        }
    }

    private DxToneTextureResource Rent(int width, int height)
    {
        var key = new ToneTextureKey(width, height);
        lock (_gate)
        {
            if (_available.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
        }

        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        var texture = _device.Device.CreateTexture2D(desc);
        var srv = _device.Device.CreateShaderResourceView(texture);
        return new DxToneTextureResource(texture, srv, width, height);
    }

    private void Return(DxToneTextureResource resource)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                resource.Dispose();
                return;
            }

            var key = new ToneTextureKey(resource.Width, resource.Height);
            if (!_available.TryGetValue(key, out var stack))
            {
                stack = new Stack<DxToneTextureResource>();
                _available[key] = stack;
            }

            if (stack.Count >= _maxRetainedPerKey)
            {
                resource.Dispose();
                return;
            }

            stack.Push(resource);
        }
    }

    private void AddToContentCache(ToneTextureContentKey key, DxToneTextureResource resource)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                resource.Dispose();
                return;
            }

            if (_contentCache.TryGetValue(key, out var existing))
            {
                existing.Resource.Dispose();
                _contentLru.Remove(existing.Node);
            }

            var node = _contentLru.AddLast(key);
            _contentCache[key] = new ToneTextureCacheEntry(resource, node);

            while (_contentCache.Count > _maxCachedContent && _contentLru.First is { } first)
            {
                var evictKey = first.Value;
                _contentLru.RemoveFirst();
                if (_contentCache.Remove(evictKey, out var evicted))
                {
                    evicted.Resource.Dispose();
                }
            }
        }
    }

    private void Touch(ToneTextureCacheEntry entry)
    {
        _contentLru.Remove(entry.Node);
        _contentLru.AddLast(entry.Node);
    }

    private static ulong ComputeHash(ReadOnlySpan<byte> bytes)
    {
        return HashMath.Fnv1A(bytes);
    }

    private readonly record struct ToneTextureKey(int Width, int Height);

    private readonly record struct ToneTextureContentKey(int Width, int Height, int ByteLength, ulong Hash);

    private sealed record ToneTextureCacheEntry(
        DxToneTextureResource Resource,
        LinkedListNode<ToneTextureContentKey> Node);
}
