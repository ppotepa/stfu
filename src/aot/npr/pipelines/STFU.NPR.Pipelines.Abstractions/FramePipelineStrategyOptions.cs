namespace STFU.NPR.Pipelines.Abstractions;

public sealed record FramePipelineStrategyOptions
{
    public static FramePipelineStrategyOptions Default { get; } = new();

    public bool EnableDiagnostics { get; init; } = true;
    public bool EnableInteractiveRuntime { get; init; } = true;
    public bool ForceReferenceFallback { get; init; }
    public bool EnableProjectionStage { get; init; } = true;
    public bool EnableVisibilityStage { get; init; } = true;
    public bool EnableCandidateEdgeStage { get; init; } = true;
    public bool EnableStrokePlanningStage { get; init; }
    public bool EnableTonePlanningStage { get; init; }
    public double TargetFrameMs { get; init; } = 16.6;
}
