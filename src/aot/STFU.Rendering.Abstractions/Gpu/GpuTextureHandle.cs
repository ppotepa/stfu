namespace STFU.Rendering.Abstractions.Gpu;

public readonly record struct GpuTextureHandle(
    string BackendId,
    long ResourceId,
    int Width,
    int Height,
    GpuSurfaceFormat Format,
    GpuTextureUsage Usage)
{
    public static GpuTextureHandle None { get; } = new(
        string.Empty,
        0,
        0,
        0,
        GpuSurfaceFormat.Bgra8888Unorm,
        GpuTextureUsage.None);

    public bool IsValid => !string.IsNullOrWhiteSpace(BackendId) && ResourceId != 0 && Width > 0 && Height > 0;
}
