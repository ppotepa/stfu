namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractivePerformanceRunSummary
{
    public required string Strategy { get; init; }
    public required string Scenario { get; init; }
    public int FrameCount { get; init; }
    public double AverageTotalMs { get; init; }
    public double P95TotalMs { get; init; }
    public double AverageProjectionMs { get; init; }
    public double AverageVisibilityMs { get; init; }
    public double AverageCandidateMs { get; init; }
    public double AverageStrokeMs { get; init; }
    public double AverageToneMs { get; init; }
    public double InteractiveReturnRatio { get; init; }
    public double ReferenceFallbackRatio { get; init; }
    public double SelfContainedProjectionRatio { get; init; }
    public double ProjectedTriangleCandidateRatio { get; init; }
    public double AverageHealthScore { get; init; }

    public bool HasFrames => FrameCount > 0;
}
