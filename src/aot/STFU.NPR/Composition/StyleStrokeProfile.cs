using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Composition;

public sealed record StyleStrokeProfile(
    NprStrokeIntent Intent,
    float BaseThickness,
    float BaseOpacity,
    StrokeColor Color,
    FeatureCurveKind? Kind = null,
    string? LayerName = null,
    StrokeMedium? MediumOverride = null,
    float HumanizationScale = 1f,
    float ThicknessVariationScale = 1f,
    float EndpointJitterScale = 1f,
    float OvershootScale = 1f);
