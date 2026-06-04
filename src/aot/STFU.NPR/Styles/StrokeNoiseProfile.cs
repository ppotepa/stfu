namespace STFU.NPR.Styles;

public sealed record StrokeNoiseProfile(
    float EndpointJitterScale,
    float TangentialJitterScale,
    float MidpointBendScale,
    float ThicknessVariationScale,
    float OpacityVariationScale);
