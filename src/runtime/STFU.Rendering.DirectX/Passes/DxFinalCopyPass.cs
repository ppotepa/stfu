using STFU.Rendering.DirectX.Device;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxFinalCopyPass
{
    private readonly DirectXDevice _device;

    public DxFinalCopyPass(DirectXDevice device)
    {
        _device = device;
    }

    public void Copy(DirectXTextureResource source, DirectXTextureResource destination)
    {
        _device.Context.CopyResource(destination.Texture, source.Texture);
    }
}
