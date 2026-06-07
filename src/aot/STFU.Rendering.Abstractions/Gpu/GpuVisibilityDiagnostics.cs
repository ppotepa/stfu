namespace STFU.Rendering.Abstractions.Gpu;

public readonly record struct GpuVisibilityDiagnostics(
    bool Requested,
    bool UsedGpuVisibilityBuffer,
    int FaceCount,
    int VisibleFaceCount,
    int VisibleFaceBitsetBytes,
    string Mode)
{
    public static GpuVisibilityDiagnostics CpuReference(int faceCount, int visibleFaceCount)
    {
        return new GpuVisibilityDiagnostics(
            Requested: false,
            UsedGpuVisibilityBuffer: false,
            FaceCount: faceCount,
            VisibleFaceCount: visibleFaceCount,
            VisibleFaceBitsetBytes: faceCount <= 0 ? 0 : (faceCount + 7) / 8,
            Mode: "CpuReference");
    }
}
