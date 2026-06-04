using STFU.NPR.Graph;

namespace STFU.NPR.Styles;

public sealed class NprStrokeStyle
{
    public int Seed { get; set; } = 1337;

    public StrokeMedium Medium { get; set; } = StrokeMedium.Ink;

    public float BaseThickness { get; set; } = 1.35f;

    public float ThicknessVariation { get; set; } = 0.55f;

    public float EndpointJitter { get; set; } = 1.15f;

    public float Overshoot { get; set; } = 2.25f;
}
