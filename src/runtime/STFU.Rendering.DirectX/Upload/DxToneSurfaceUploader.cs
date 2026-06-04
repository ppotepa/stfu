using System.Runtime.InteropServices;
using STFU.NPR.Rendering;
using STFU.Rendering.DirectX.Device;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace STFU.Rendering.DirectX.Upload;

public sealed class DxToneSurfaceUploader
{
    private readonly DirectXDevice _device;

    public DxToneSurfaceUploader(DirectXDevice device)
    {
        _device = device;
    }

    public unsafe DxToneSurfaceUpload? Upload(NprToneSurface2D tone)
    {
        if (tone.Width <= 0 ||
            tone.Height <= 0 ||
            tone.Rgba.Length < tone.Width * tone.Height * 4)
        {
            return null;
        }

        var desc = new Texture2DDescription
        {
            Width = (uint)tone.Width,
            Height = (uint)tone.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        fixed (byte* rgbaPtr = tone.Rgba)
        {
            var data = new SubresourceData((IntPtr)rgbaPtr, (uint)(tone.Width * 4), 0);
            var texture = _device.Device.CreateTexture2D(desc, data);
            var srv = _device.Device.CreateShaderResourceView(texture);
            return new DxToneSurfaceUpload(tone, texture, srv);
        }
    }
}
