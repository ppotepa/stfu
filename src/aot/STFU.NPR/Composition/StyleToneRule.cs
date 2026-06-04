namespace STFU.NPR.Composition;

public sealed record StyleToneRule(
    bool Enabled,
    float ToneInfluence,
    float ShadeInfluence,
    float MinimumOpacity,
    float MaximumOpacity);
