using STFU.NPR.Pipelines.Abstractions;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveFrameDiagnostics
{
    private readonly Dictionary<string, double> _stageTimingsMs = [];

    public FramePipelineStrategy Strategy { get; set; }
    public InteractiveWorkClass WorkClass { get; set; }

    public long FrameId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public ulong ContentHash { get; set; }
    public ulong CameraHash { get; set; }
    public ulong StyleHash { get; set; }
    public ulong ViewportHash { get; set; }
    public ulong DebugHash { get; set; }
    public bool CameraChanged { get; set; }
    public bool SceneChanged { get; set; }
    public bool AnimationChanged { get; set; }
    public bool StyleChanged { get; set; }
    public bool ViewportSizeChanged { get; set; }
    public bool DebugOverlayChanged { get; set; }
    public InteractiveQualityMode QualityMode { get; set; }
    public int ArtifactStoreItemCount { get; set; }
    public int FrameOrCameraArtifactCount { get; set; }
    public int PrunedFrameOrCameraArtifactCount { get; set; }
    public int StaticArtifactCount { get; set; }
    public int SceneArtifactCount { get; set; }
    public int SessionArtifactCount { get; set; }

    public double ProjectionMs { get; set; }
    public double VisibilityMs { get; set; }
    public double CandidateMs { get; set; }
    public double StrokePlanMs { get; set; }
    public double TonePlanMs { get; set; }
    public double GpuUploadMs { get; set; }
    public double GpuDrawMs { get; set; }

    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public long ProjectionSource { get; set; }
    public bool ProjectionBuiltSelfContained { get; set; }
    public int ProjectionSourceEntities { get; set; }
    public int ProjectionMeshes { get; set; }
    public int ProjectedVertices { get; set; }
    public int ProjectedTriangles { get; set; }
    public int VisibleProjectedVertices { get; set; }
    public int VisibleProjectedTriangles { get; set; }
    public int FrontFacingProjectedTriangles { get; set; }
    public long VisibilitySource { get; set; }
    public string VisibilityProviderName { get; set; } = string.Empty;
    public int VisibilitySourceProjectedTriangles { get; set; }
    public bool VisibilityUsedProjectedTriangles => VisibilitySource == (long)InteractiveVisibilitySource.ProjectedTriangles;
    public int TotalFaces { get; set; }
    public int VisibleFaces { get; set; }
    public double VisibleFaceRatioPercent { get; set; }
    public int TotalEdges { get; set; }
    public int CandidateEdges { get; set; }
    public double CandidateReductionPercent { get; set; }
    public long CandidateEdgeSource { get; set; }
    public int CandidateEdgeSourceReferenceFragments { get; set; }
    public int CandidateEdgeSourceProjectedTriangles { get; set; }
    public bool CandidateEdgesBuiltFromProjectedTriangles => CandidateEdgeSource == (long)InteractiveCandidateEdgeSource.ProjectedTriangleEdges;
    public int CandidateEdgesBeforeBudget { get; set; }
    public int CandidateEdgesAfterBudget { get; set; }
    public bool CandidateEdgeBudgetApplied { get; set; }
    public int VisibleSegments { get; set; }
    public int VisibleSegmentSourceCommands { get; set; }
    public double VisibleSegmentCoveragePercent { get; set; }
    public int VisibleSegmentsBeforeBudget { get; set; }
    public int VisibleSegmentsAfterBudget { get; set; }
    public bool VisibleSegmentBudgetApplied { get; set; }
    public int InteractiveStrokeFrameSourceSegments { get; set; }
    public int InteractiveStrokeFramePaths { get; set; }
    public int InteractiveStrokeFrameSegments { get; set; }
    public double InteractiveStrokeFrameCoveragePercent { get; set; }
    public int TotalStrokeCandidates { get; set; }
    public int StrokeCommands { get; set; }
    public double StrokeCommandReductionPercent { get; set; }
    public int StrokeCommandsBeforeBudget { get; set; }
    public int StrokeCommandsAfterBudget { get; set; }
    public bool StrokeCommandBudgetApplied { get; set; }
    public bool TonePlanningDeferred { get; set; }
    public int ToneSourceFaces { get; set; }
    public int ToneRegions { get; set; }
    public double ToneCoverageRatioPercent { get; set; }
    public int ToneHighlightRegions { get; set; }
    public int ToneMidtoneRegions { get; set; }
    public int ToneShadowRegions { get; set; }
    public InteractiveOutputKind OutputKind { get; set; } = InteractiveOutputKind.None;
    public bool InteractivePreviewCandidateAvailable { get; set; }
    public InteractiveOutputReadiness OutputReadiness { get; set; } = InteractiveOutputReadiness.None;
    public int OutputReadinessScore { get; set; }
    public int OutputProjectedVertices { get; set; }
    public int OutputProjectedTriangles { get; set; }
    public int OutputVisibleFaces { get; set; }
    public int OutputCandidateEdges { get; set; }
    public int OutputStrokeCommands { get; set; }
    public int OutputVisibleStrokeSegments { get; set; }
    public int OutputInteractiveStrokeFramePaths { get; set; }
    public int OutputInteractiveStrokeFrameSegments { get; set; }
    public int OutputToneRegions { get; set; }
    public string OutputReason { get; set; } = string.Empty;

    public InteractivePreviewDecisionKind PreviewDecision { get; set; } = InteractivePreviewDecisionKind.Unknown;
    public int PreviewCandidateReadinessScore { get; set; }
    public int PreviewMinimumReadinessScore { get; set; }
    public bool PreviewRejectedByReadinessGate { get; set; }
    public bool PreviewRejectedBySegmentBudget { get; set; }
    public bool ReturnedInteractiveFrame { get; set; }
    public bool ReturnedReferenceFallback { get; set; }
    public int ReturnedInteractiveFramePaths { get; set; }
    public int ReturnedInteractiveFrameSegments { get; set; }
    public string FinalOutputReason { get; set; } = string.Empty;

    public InteractiveOutputHealthStatus OutputHealthStatus { get; set; } = InteractiveOutputHealthStatus.Unknown;
    public int OutputHealthScore { get; set; }
    public int OutputHealthWarningCount { get; set; }
    public string OutputHealthSummary { get; set; } = string.Empty;

    public bool UsedReferenceFallback { get; set; }
    public string FallbackReason { get; set; } = string.Empty;

    public InteractiveReferenceExecutionMode ReferenceExecutionMode { get; set; } = InteractiveReferenceExecutionMode.BeforeInteractive;
    public bool ReferenceExecutedBeforeInteractive { get; set; }
    public bool ReferenceExecutedAfterInteractive { get; set; }
    public bool ReferenceExecutionSkipped { get; set; }
    public bool ReferenceExecutionDisabledForPreview { get; set; }
    public bool ReferenceFallbackFrameAvailable { get; set; }
    public bool ReferenceFallbackUnavailable { get; set; }
    public bool ReferenceFallbackEmptyFrame { get; set; }
    public string ReferenceExecutionReason { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, double> StageTimingsMs => _stageTimingsMs;


    public void CaptureIntent(InteractiveFrameIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        FrameId = intent.FrameId;
        Width = intent.Width;
        Height = intent.Height;
        ContentHash = intent.Signature.ContentHash;
        CameraHash = intent.Signature.CameraHash;
        StyleHash = intent.Signature.StyleHash;
        ViewportHash = intent.Signature.ViewportHash;
        DebugHash = intent.Signature.DebugHash;
        CameraChanged = intent.CameraChanged;
        SceneChanged = intent.SceneChanged;
        AnimationChanged = intent.AnimationChanged;
        StyleChanged = intent.StyleChanged;
        ViewportSizeChanged = intent.ViewportSizeChanged;
        DebugOverlayChanged = intent.DebugOverlayChanged;
        QualityMode = intent.QualityMode;
    }


    public void CaptureReferenceExecution(
        InteractiveReferenceExecutionPolicy policy,
        bool executedBeforeInteractive,
        bool executedAfterInteractive,
        bool fallbackFrameAvailable)
    {
        ArgumentNullException.ThrowIfNull(policy);

        ReferenceExecutionMode = policy.Mode;
        ReferenceExecutedBeforeInteractive = executedBeforeInteractive;
        ReferenceExecutedAfterInteractive = executedAfterInteractive;
        ReferenceExecutionSkipped = !executedBeforeInteractive && !executedAfterInteractive;
        ReferenceExecutionDisabledForPreview = policy.ReferenceDisabledForPreview;
        ReferenceFallbackFrameAvailable = fallbackFrameAvailable;
        ReferenceFallbackUnavailable = ReferenceFallbackUnavailable || (!fallbackFrameAvailable && !executedBeforeInteractive && !executedAfterInteractive);
        ReferenceExecutionReason = policy.Reason;
    }

    public void CaptureOutput(InteractiveOutputSummary output)
    {
        ArgumentNullException.ThrowIfNull(output);

        OutputKind = output.Kind;
        InteractivePreviewCandidateAvailable = output.IsInteractivePreviewCandidate;
        OutputReadiness = output.Readiness;
        OutputReadinessScore = output.ReadinessScore;
        OutputProjectedVertices = output.ProjectedVertexCount;
        OutputProjectedTriangles = output.ProjectedTriangleCount;
        OutputVisibleFaces = output.VisibleFaceCount;
        OutputCandidateEdges = output.CandidateEdgeCount;
        OutputStrokeCommands = output.StrokeCommandCount;
        OutputVisibleStrokeSegments = output.VisibleStrokeSegmentCount;
        OutputInteractiveStrokeFramePaths = output.InteractiveStrokeFramePathCount;
        OutputInteractiveStrokeFrameSegments = output.InteractiveStrokeFrameSegmentCount;
        OutputToneRegions = output.ToneRegionCount;
        OutputReason = output.Reason;
    }

    public void CaptureOutputHealth(InteractiveOutputHealthReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        OutputHealthStatus = report.Status;
        OutputHealthScore = report.Score;
        OutputHealthWarningCount = report.WarningCount;
        OutputHealthSummary = report.Summary;
    }

    public void AddStageTiming(string stageName, TimeSpan elapsed)
    {
        if (string.IsNullOrWhiteSpace(stageName))
        {
            return;
        }

        var ms = elapsed.TotalMilliseconds;
        _stageTimingsMs[stageName] = ms;

        switch (stageName)
        {
            case "InteractiveProjection":
                ProjectionMs = ms;
                break;
            case "InteractiveVisibility":
                VisibilityMs = ms;
                break;
            case "InteractiveCandidateEdges":
                CandidateMs = ms;
                break;
            case "InteractiveStrokePlanning":
                StrokePlanMs = ms;
                break;
            case "InteractiveVisibleStrokeSegments":
                StrokePlanMs += ms;
                break;
            case "InteractiveStrokeFrame":
                StrokePlanMs += ms;
                break;
            case "InteractiveTonePlanning":
                TonePlanMs = ms;
                break;
            case "InteractiveGpuUpload":
                GpuUploadMs = ms;
                break;
            case "InteractiveGpuDraw":
                GpuDrawMs = ms;
                break;
        }
    }
}
