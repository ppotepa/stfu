namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveRuntimeGateSnapshotBuilder
{
    public static InteractivePerformanceRunSummary BuildInteractiveSummary(
        string scenario,
        InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return InteractiveRuntimeEvidenceBuilder.BuildInteractiveSummary(scenario, diagnostics);
    }

    public static InteractivePerformanceRunSummary BuildReferenceBaseline(
        string scenario,
        double referenceAverageMs,
        double healthScore = 100d)
    {
        return InteractiveRuntimeEvidenceBuilder.BuildReferenceBaseline(scenario, referenceAverageMs, healthScore);
    }

    public static InteractiveRunComparisonSnapshot BuildComparison(
        string scenario,
        double referenceAverageMs,
        InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return InteractiveRuntimeEvidenceBuilder.BuildComparison(scenario, referenceAverageMs, diagnostics);
    }
}
