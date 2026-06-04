namespace STFU.NPR.Composition;

public sealed record StyleVisibilityRule(
    VisibilityStrictness Strictness,
    float DepthBias,
    bool SplitCurves,
    bool KeepHiddenSegmentsForDebug,
    HiddenLinePolicy DefaultHiddenPolicy);
