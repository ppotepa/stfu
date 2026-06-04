namespace STFU.NPR.Visibility;

public sealed record VisibilityOptions(
    float DepthBias,
    bool SplitCurves,
    bool KeepHiddenSegmentsForDebug)
{
    public static VisibilityOptions Default { get; } = new(0.025f, SplitCurves: true, KeepHiddenSegmentsForDebug: true);
}
