using STFU.NPR.Analysis;

namespace STFU.Rendering.Abstractions.Requests;

public sealed record NprQualityProfile(
    bool AntialiasLines = true,
    bool RasterizeToneSurfaces = true,
    bool RasterizeGrid = true,
    bool RasterizeMeshWireframe = true,
    float LineCoverageSoftness = 1.0f,
    bool PreserveLayerOrdering = true,
    bool UseGpuStrokeRaster = true,
    bool UseGpuToneRaster = true,
    bool UseGpuDebugOverlayRaster = true,
    bool UseGpuMeshWireframe = true,
    bool UseGpuVisibilityBuffer = false,
    GpuMeshWireframePath GpuMeshWireframePath = GpuMeshWireframePath.Native,
    float GpuStrokeCoverageSoftness = 1.0f,
    MeshWireframeTopologyMode MeshWireframeTopologyMode = MeshWireframeTopologyMode.Welded)
{
    public static NprQualityProfile Default { get; } = new();
}
