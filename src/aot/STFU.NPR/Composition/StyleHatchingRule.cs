namespace STFU.NPR.Composition;

public sealed record StyleHatchingRule(
    bool Enabled,
    float ToneThreshold,
    float CrossHatchThreshold,
    float DeepShadowThreshold,
    float DensityScale,
    float BaseSpacingPixels,
    float StrokeLengthPixels,
    float DirectionAngleOffsetRadians,
    float CrossAngleOffsetRadians,
    float TertiaryAngleOffsetRadians,
    float JitterRadians,
    bool UseDirectionField);
