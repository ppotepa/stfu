namespace STFU.Rendering.Abstractions.Gpu;

[Flags]
public enum GpuTextureUsage
{
    None = 0,
    RenderTarget = 1 << 0,
    ShaderResource = 1 << 1,
    TransferSource = 1 << 2,
    TransferDestination = 1 << 3,
    Presentable = 1 << 4
}
