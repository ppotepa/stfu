using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXPipelineStates : IDisposable
{
    public DirectXPipelineStates(ID3D11Device device)
    {
        var blendDescription = BlendDescription.Opaque;
        blendDescription.AlphaToCoverageEnable = false;
        blendDescription.IndependentBlendEnable = false;
        blendDescription.RenderTarget[0] = new RenderTargetBlendDescription
        {
            BlendEnable = true,
            SourceBlend = Blend.One,
            DestinationBlend = Blend.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.InverseSourceAlpha,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };

        PremultipliedAlphaBlend = device.CreateBlendState(blendDescription);
        NoCullRasterizer = device.CreateRasterizerState(RasterizerDescription.CullNone);
        DepthDisabled = device.CreateDepthStencilState(DepthStencilDescription.None);
    }

    public ID3D11BlendState PremultipliedAlphaBlend { get; }

    public ID3D11RasterizerState NoCullRasterizer { get; }

    public ID3D11DepthStencilState DepthDisabled { get; }

    public void Dispose()
    {
        DepthDisabled.Dispose();
        NoCullRasterizer.Dispose();
        PremultipliedAlphaBlend.Dispose();
    }
}
