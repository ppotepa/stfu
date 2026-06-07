namespace STFU.Rendering.Abstractions.Gpu;

public enum GpuVisibilityFallbackMode
{
    Disabled,
    FallbackOnMismatch,
    FallbackOnUnsupported,
    AlwaysCpuReference
}
