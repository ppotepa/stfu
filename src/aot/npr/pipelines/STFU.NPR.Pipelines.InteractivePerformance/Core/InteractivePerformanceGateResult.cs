namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractivePerformanceGateResult
{
    public InteractivePerformanceGateStatus Status { get; init; }
    public required string Scenario { get; init; }
    public double ReferenceAverageMs { get; init; }
    public double InteractiveAverageMs { get; init; }
    public double SpeedupRatio { get; init; }
    public double InteractiveReturnRatio { get; init; }
    public double ReferenceFallbackRatio { get; init; }
    public double SelfContainedProjectionRatio { get; init; }
    public double ProjectedTriangleCandidateRatio { get; init; }
    public double AverageHealthScore { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Failures { get; init; } = [];

    public bool Passed => Status == InteractivePerformanceGateStatus.Pass;
}
