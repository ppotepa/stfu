using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using Vortice.Mathematics;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxClearPass
{
    private readonly DirectXDevice _device;

    public DxClearPass(DirectXDevice device)
    {
        _device = device;
    }

    public void Execute(DirectXTextureResource target, NprRenderTheme theme)
    {
        if (target.RenderTargetView is null)
        {
            throw new InvalidOperationException("Clear target has no RTV.");
        }

        var color = theme.PaperColor;
        var rgba = new Color4(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            1f);

        _device.Context.ClearRenderTargetView(target.RenderTargetView, rgba);
    }
}
