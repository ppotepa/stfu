namespace STFU.Rendering.Abstractions.Gpu;

public readonly record struct GpuVisibilityBufferRequest(
    int Width,
    int Height,
    int FaceCount,
    bool RequireVisibleFaceReadback)
{
    public int VisibleFaceBitsetBytes => FaceCount <= 0 ? 0 : (FaceCount + 7) / 8;
}
