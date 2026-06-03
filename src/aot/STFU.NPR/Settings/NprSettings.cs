using STFU.NPR.Styles;

namespace STFU.NPR.Settings;

public sealed class NprSettings
{
    public int Seed { get; set; } = 1337;

    public float CreaseAngleDegrees { get; set; } = 35f;

    public float MinimumProjectedTriangleArea { get; set; } = 8f;

    public float MinimumStrokeLength { get; set; } = 4f;

    public float SurfaceFlowShadeThreshold { get; set; } = 0.55f;

    public float SurfaceFlowDensity { get; set; } = 0.42f;

    public float HatchShadeThreshold { get; set; } = 0.62f;

    public float HatchDensity { get; set; } = 0.5f;

    public float HatchLength { get; set; } = 18f;

    public float HiddenLineDepthBias { get; set; } = 0.025f;

    public float FeatureLineDensity { get; set; } = 0.82f;

    public NprStrokeStyle StrokeStyle { get; } = new();
}
