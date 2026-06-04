using STFU.NPR.Graph;

namespace STFU.NPR.Composition;

public sealed record StyleFeatureRule(
    FeatureCurveKind Kind,
    bool Enabled,
    float BaseWeight,
    float MinSalience,
    HiddenLinePolicy HiddenLinePolicy,
    NprStrokeIntent Intent,
    int LayerOrder,
    string LayerName);
