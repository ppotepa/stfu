namespace STFU.NPR.Pipelines.Abstractions;

public sealed record FramePipelineStrategyOptions
{
    public static FramePipelineStrategyOptions Default { get; } = new();

    public bool EnableDiagnostics { get; init; } = true;
    public bool EnableInteractiveRuntime { get; init; } = true;
    public bool ForceReferenceFallback { get; init; }
    public bool EnableProjectionStage { get; init; } = true;
    public bool EnableSelfContainedProjection { get; init; } = true;
    public bool PreferSelfContainedProjection { get; init; }
    public bool EnableVisibilityStage { get; init; } = true;
    public bool EnableProjectedTriangleVisibility { get; init; } = true;
    public bool RequireFrontFacingProjectedTriangleVisibility { get; init; } = true;
    public bool EnableCandidateEdgeStage { get; init; } = true;
    public bool EnableStrokePlanningStage { get; init; } = true;
    public bool EnableVisibleStrokeSegmentStage { get; init; } = true;
    public bool EnableInteractiveStrokeFrameStage { get; init; } = true;
    public bool EnableTonePlanningStage { get; init; } = true;
    public bool EnableInteractiveOutputContract { get; init; } = true;
    public bool EnableInteractivePreviewOutput { get; init; }
    public bool EnableReferenceFreeInteractivePreview { get; init; }
    public bool UseReferenceFallbackForFinalFrame { get; init; } = true;
    public bool RequireToneCoverageForInteractivePreview { get; init; }
    public int InteractivePreviewMaxStrokeSegments { get; init; }

    public int InteractivePreviewMinReadinessScore { get; init; }
    public int MaxInteractiveCandidateEdges { get; init; }
    public int MaxInteractiveStrokeCommands { get; init; }
    public int MaxInteractiveVisibleStrokeSegments { get; init; }
    public bool DeferToneCoverageWhenPreviewDoesNotRequireTone { get; init; }
    public int MaxFrameOrCameraArtifactsPerKind { get; init; } = 3;
    public int MaxTotalFrameOrCameraArtifacts { get; init; } = 64;
    public double TargetFrameMs { get; init; } = 16.6;
}