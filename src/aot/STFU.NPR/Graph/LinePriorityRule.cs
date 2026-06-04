using STFU.NPR.Composition;

namespace STFU.NPR.Graph;

public sealed record LinePriorityRule(
    FeatureCurveKind Kind,
    float BaseWeight,
    float MinScreenLength,
    float MaxDensityPerTile,
    HiddenLinePolicy HiddenPolicy,
    bool AlwaysKeepIfOuterSilhouette);
