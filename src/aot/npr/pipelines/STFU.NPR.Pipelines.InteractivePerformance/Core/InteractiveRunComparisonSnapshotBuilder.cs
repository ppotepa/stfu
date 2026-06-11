namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveRunComparisonSnapshotBuilder
{
    public static InteractiveRunComparisonSnapshot Build(
        string scenario,
        InteractivePerformanceRunSummary reference,
        InteractivePerformanceRunSummary interactive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(interactive);

        var speedup = reference.AverageTotalMs > 0d && interactive.AverageTotalMs > 0d
            ? reference.AverageTotalMs / interactive.AverageTotalMs
            : 0d;

        return new InteractiveRunComparisonSnapshot(
            scenario,
            reference.AverageTotalMs,
            interactive.AverageTotalMs,
            speedup,
            interactive.ReferenceFallbackRatio,
            interactive.InteractiveReturnRatio);
    }
}
