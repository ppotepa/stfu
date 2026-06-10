namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveFrameBenchmarkThresholds(
    double TargetFrameMs = 16.6d,
    double WarningFrameMs = 24d,
    double FailFrameMs = 33.3d,
    double MinimumInteractiveReturnRatio = 0.50d,
    double MaximumReferenceFallbackRatio = 0.50d,
    int MinimumHealthScore = 60)
{
    public static InteractiveFrameBenchmarkThresholds Default { get; } = new();

    public InteractiveFrameBenchmarkStatus Evaluate(InteractiveFrameBenchmarkReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.SampleCount == 0)
        {
            return InteractiveFrameBenchmarkStatus.Unknown;
        }

        if (report.AverageStageMs >= FailFrameMs ||
            report.ReferenceFallbackRatio > MaximumReferenceFallbackRatio ||
            report.AverageHealthScore < MinimumHealthScore)
        {
            return InteractiveFrameBenchmarkStatus.Fail;
        }

        if (report.AverageStageMs >= WarningFrameMs ||
            report.InteractiveReturnRatio < MinimumInteractiveReturnRatio)
        {
            return InteractiveFrameBenchmarkStatus.Warning;
        }

        return InteractiveFrameBenchmarkStatus.Pass;
    }
}
