using STFU.NPR.Rendering;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Upload;

public sealed class DxToneSurfaceUpload : IDisposable
{
    public DxToneSurfaceUpload(NprToneSurface2D source, ID3D11Texture2D texture, ID3D11ShaderResourceView shaderResourceView)
    {
        Source = source;
        Texture = texture;
        ShaderResourceView = shaderResourceView;
    }

    public NprToneSurface2D Source { get; }

    public ID3D11Texture2D Texture { get; }

    public ID3D11ShaderResourceView ShaderResourceView { get; }

    public void Dispose()
    {
        ShaderResourceView.Dispose();
        Texture.Dispose();
    }
}
