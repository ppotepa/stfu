namespace STFU.NPR.Graph;

public sealed record HatchLayer(
    float ToneThreshold,
    float SpacingPixels,
    float StrokeLengthPixels,
    float DirectionAngleOffsetRadians,
    float Opacity,
    float Thickness,
    HatchLayerKind Kind);
