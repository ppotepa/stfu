namespace STFU.Rendering.Abstractions.Gpu;

public readonly record struct GpuVisibilityBufferHandle(
    GpuTextureHandle FaceIdTexture,
    GpuTextureHandle DepthTexture,
    int FaceCount)
{
    public static GpuVisibilityBufferHandle None { get; } = new(
        GpuTextureHandle.None,
        GpuTextureHandle.None,
        0);

    public bool IsValid => FaceIdTexture.IsValid && FaceCount > 0;
}
