namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveBenchmarkComparisonThresholds(
    double MinimumSpeedupRatio = 1.05d,
    double WarningSpeedupRatio = 0.95d,
    double MaximumStageRatio = 1.10d,
    double MaximumReferenceFallbackRatio = 0.35d,
    double MinimumInteractiveReturnRatio = 0.65d,
    double MinimumSelfContainedProjectionRatio = 0.50d,
    double MinimumProjectedTriangleCandidateRatio = 0.50d,
    int MinimumHealthScore = 65)
{
    public static InteractiveBenchmarkComparisonThresholds Default { get; } = new();

    public InteractiveBenchmarkComparisonStatus Evaluate(InteractiveBenchmarkComparisonReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Reference.SampleCount == 0 || report.Interactive.SampleCount == 0)
        {
            return InteractiveBenchmarkComparisonStatus.Unknown;
        }

        if (report.InteractiveStageRatio > MaximumStageRatio ||
            report.Interactive.ReferenceFallbackRatio > MaximumReferenceFallbackRatio ||
            report.Interactive.InteractiveReturnRatio < MinimumInteractiveReturnRatio ||
            report.Interactive.AverageHealthScore < MinimumHealthScore)
        {
            return InteractiveBenchmarkComparisonStatus.Fail;
        }

        if (report.SpeedupRatio < WarningSpeedupRatio ||
            report.Interactive.SelfContainedProjectionRatio < MinimumSelfContainedProjectionRatio ||
            report.Interactive.ProjectedTriangleCandidateRatio < MinimumProjectedTriangleCandidateRatio)
        {
            return InteractiveBenchmarkComparisonStatus.Warning;
        }

        return report.SpeedupRatio >= MinimumSpeedupRatio
            ? InteractiveBenchmarkComparisonStatus.Pass
            : InteractiveBenchmarkComparisonStatus.Warning;
    }
}
