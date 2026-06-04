using STFU.Rendering.Abstractions.Gpu;
using STFU.Rendering.DirectX.Backend;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXResourceRegistry : IDisposable
{
    private readonly Dictionary<long, DirectXTextureResource> _textures = new();
    private bool _disposed;

    public void Register(DirectXTextureResource texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _textures[texture.Handle.ResourceId] = texture;
    }

    public bool TryGetTexture(GpuTextureHandle handle, out DirectXTextureResource texture)
    {
        if (handle.BackendId != DirectXRenderBackend.BackendId)
        {
            texture = default!;
            return false;
        }

        return _textures.TryGetValue(handle.ResourceId, out texture!);
    }

    public void Unregister(GpuTextureHandle handle)
    {
        if (_textures.Remove(handle.ResourceId, out var texture))
        {
            texture.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
    }
}

public sealed class DirectXTextureResource : IDisposable
{
    public DirectXTextureResource(
        GpuTextureHandle handle,
        ID3D11Texture2D texture,
        ID3D11RenderTargetView? renderTargetView,
        ID3D11ShaderResourceView? shaderResourceView)
    {
        Handle = handle;
        Texture = texture;
        RenderTargetView = renderTargetView;
        ShaderResourceView = shaderResourceView;
    }

    public GpuTextureHandle Handle { get; }

    public ID3D11Texture2D Texture { get; }

    public ID3D11RenderTargetView? RenderTargetView { get; }

    public ID3D11ShaderResourceView? ShaderResourceView { get; }

    public void Dispose()
    {
        ShaderResourceView?.Dispose();
        RenderTargetView?.Dispose();
        Texture.Dispose();
    }
}
