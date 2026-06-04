namespace STFU.Rendering.Abstractions.Backend;

[Flags]
public enum NprBackendCapabilities
{
    None = 0,

    CpuSingleThread = 1 << 0,
    CpuParallel = 1 << 1,
    CpuTileRaster = 1 << 2,
    CpuToneRaster = 1 << 3,
    CpuStrokeRaster = 1 << 4,
    CpuMeshWireframe = 1 << 5,

    GpuGraphics = 1 << 8,
    GpuCompute = 1 << 9,
    GpuPresentation = 1 << 10,
    GpuReadback = 1 << 11,
    GpuRenderTargets = 1 << 12,
    GpuTextureUpload = 1 << 13,
    GpuStrokeRaster = 1 << 14,
    GpuToneRaster = 1 << 15,
    GpuMeshWireframe = 1 << 16,
    GpuDebugOverlay = 1 << 17,
    GpuFinalComposite = 1 << 18,

    NprPipelineExecution = 1 << 24,
    PixelSurfaceOutput = 1 << 25,
    GpuTextureOutput = 1 << 26
}
