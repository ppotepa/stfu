namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractivePerformanceGateThresholds
{
    public double TargetFrameMs { get; init; } = 16.6;
    public double WarningFrameMs { get; init; } = 24.0;
    public double FailFrameMs { get; init; } = 33.3;
    public double MinimumSpeedupRatio { get; init; } = 1.05;
    public double WarningSpeedupRatio { get; init; } = 0.95;
    public double MaximumReferenceFallbackRatio { get; init; } = 0.25;
    public double MinimumInteractiveReturnRatio { get; init; } = 0.65;
    public double MinimumSelfContainedProjectionRatio { get; init; } = 0.60;
    public double MinimumProjectedTriangleCandidateRatio { get; init; } = 0.60;
    public double MinimumHealthScore { get; init; } = 60.0;
}
