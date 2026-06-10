namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveBenchmarkComparisonReport
{
    public InteractiveBenchmarkComparisonReport(
        InteractiveFrameBenchmarkReport reference,
        InteractiveFrameBenchmarkReport interactive)
        : this(reference, interactive, InteractiveBenchmarkComparisonThresholds.Default)
    {
    }

    public InteractiveBenchmarkComparisonReport(
        InteractiveFrameBenchmarkReport reference,
        InteractiveFrameBenchmarkReport interactive,
        InteractiveBenchmarkComparisonThresholds thresholds)
    {
        Reference = reference ?? InteractiveFrameBenchmarkReport.Empty;
        Interactive = interactive ?? InteractiveFrameBenchmarkReport.Empty;
        Thresholds = thresholds ?? InteractiveBenchmarkComparisonThresholds.Default;

        ReferenceAverageStageMs = Reference.AverageStageMs;
        InteractiveAverageStageMs = Interactive.AverageStageMs;
        StageDeltaMs = InteractiveAverageStageMs - ReferenceAverageStageMs;
        SpeedupRatio = Ratio(ReferenceAverageStageMs, InteractiveAverageStageMs);
        InteractiveStageRatio = Ratio(InteractiveAverageStageMs, ReferenceAverageStageMs);
        InteractiveReturnRatio = Interactive.InteractiveReturnRatio;
        ReferenceFallbackRatio = Interactive.ReferenceFallbackRatio;
        SelfContainedProjectionRatio = Interactive.SelfContainedProjectionRatio;
        ProjectedTriangleCandidateRatio = Interactive.ProjectedTriangleCandidateRatio;
        HealthScoreDelta = Interactive.AverageHealthScore - Reference.AverageHealthScore;
        SampleCountDelta = Interactive.SampleCount - Reference.SampleCount;
        Status = Thresholds.Evaluate(this);
    }

    public InteractiveFrameBenchmarkReport Reference { get; }
    public InteractiveFrameBenchmarkReport Interactive { get; }
    public InteractiveBenchmarkComparisonThresholds Thresholds { get; }
    public double ReferenceAverageStageMs { get; }
    public double InteractiveAverageStageMs { get; }
    public double StageDeltaMs { get; }
    public double SpeedupRatio { get; }
    public double InteractiveStageRatio { get; }
    public double InteractiveReturnRatio { get; }
    public double ReferenceFallbackRatio { get; }
    public double SelfContainedProjectionRatio { get; }
    public double ProjectedTriangleCandidateRatio { get; }
    public double HealthScoreDelta { get; }
    public int SampleCountDelta { get; }
    public InteractiveBenchmarkComparisonStatus Status { get; }

    public bool HasComparableSamples => Reference.SampleCount > 0 && Interactive.SampleCount > 0;

    private static double Ratio(double numerator, double denominator)
    {
        if (numerator <= 0d || denominator <= 0d)
        {
            return 0d;
        }

        return numerator / denominator;
    }
}
