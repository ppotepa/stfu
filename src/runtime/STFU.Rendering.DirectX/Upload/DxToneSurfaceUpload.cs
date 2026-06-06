using STFU.NPR.Rendering;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Upload;

public sealed class DxToneSurfaceUpload : IDisposable
{
    private Action<DxToneTextureResource>? _release;

    public DxToneSurfaceUpload(
        NprToneSurface2D source,
        DxToneTextureResource resource,
        Action<DxToneTextureResource> release)
    {
        Source = source;
        Resource = resource;
        _release = release;
    }

    public NprToneSurface2D Source { get; }

    public DxToneTextureResource Resource { get; }

    public ID3D11Texture2D Texture => Resource.Texture;

    public ID3D11ShaderResourceView ShaderResourceView => Resource.ShaderResourceView;

    public void Dispose()
    {
        if (_release is { } release)
        {
            _release = null;
            release(Resource);
        }
    }
}

public sealed class DxToneTextureResource : IDisposable
{
    public DxToneTextureResource(
        ID3D11Texture2D texture,
        ID3D11ShaderResourceView shaderResourceView,
        int width,
        int height)
    {
        Texture = texture;
        ShaderResourceView = shaderResourceView;
        Width = width;
        Height = height;
    }

    public ID3D11Texture2D Texture { get; }

    public ID3D11ShaderResourceView ShaderResourceView { get; }

    public int Width { get; }

    public int Height { get; }

    public void Dispose()
    {
        ShaderResourceView.Dispose();
        Texture.Dispose();
    }
}
