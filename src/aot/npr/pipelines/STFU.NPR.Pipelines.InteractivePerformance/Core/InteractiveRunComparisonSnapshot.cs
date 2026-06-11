namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveRunComparisonSnapshot(
    string Scenario,
    double ReferenceAverageMs,
    double InteractiveAverageMs,
    double SpeedupRatio,
    double FallbackRatio,
    double InteractiveReturnRatio)
{
    public bool IsMeaningful => ReferenceAverageMs > 0d && InteractiveAverageMs > 0d;
}
