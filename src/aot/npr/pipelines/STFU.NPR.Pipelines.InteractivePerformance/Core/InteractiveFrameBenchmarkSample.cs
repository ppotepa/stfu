using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveFrameBenchmarkSample(
    long FrameId,
    InteractiveQualityMode QualityMode,
    InteractiveWorkClass WorkClass,
    double TotalStageMs,
    double ProjectionMs,
    double VisibilityMs,
    double CandidateMs,
    double StrokePlanMs,
    double TonePlanMs,
    int ProjectedTriangles,
    int VisibleFaces,
    int CandidateEdges,
    int StrokeCommands,
    int VisibleStrokeSegments,
    int ToneRegions,
    bool ReturnedInteractiveFrame,
    bool ReturnedReferenceFallback,
    bool ProjectionBuiltSelfContained,
    bool CandidateEdgesBuiltFromProjectedTriangles,
    InteractiveOutputHealthStatus OutputHealthStatus,
    int OutputHealthScore,
    InteractivePreviewDecisionKind PreviewDecision,
    InteractiveBudgetPressure BudgetPressure)
{
    public static InteractiveFrameBenchmarkSample FromDiagnostics(InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new InteractiveFrameBenchmarkSample(
            FrameId: diagnostics.FrameId,
            QualityMode: diagnostics.QualityMode,
            WorkClass: diagnostics.WorkClass,
            TotalStageMs: diagnostics.TotalInteractiveStageMs,
            ProjectionMs: diagnostics.ProjectionMs,
            VisibilityMs: diagnostics.VisibilityMs,
            CandidateMs: diagnostics.CandidateMs,
            StrokePlanMs: diagnostics.StrokePlanMs,
            TonePlanMs: diagnostics.TonePlanMs,
            ProjectedTriangles: diagnostics.ProjectedTriangles,
            VisibleFaces: diagnostics.VisibleFaces,
            CandidateEdges: diagnostics.CandidateEdges,
            StrokeCommands: diagnostics.StrokeCommands,
            VisibleStrokeSegments: diagnostics.VisibleSegments,
            ToneRegions: diagnostics.ToneRegions,
            ReturnedInteractiveFrame: diagnostics.ReturnedInteractiveFrame,
            ReturnedReferenceFallback: diagnostics.ReturnedReferenceFallback,
            ProjectionBuiltSelfContained: diagnostics.ProjectionBuiltSelfContained,
            CandidateEdgesBuiltFromProjectedTriangles: diagnostics.CandidateEdgesBuiltFromProjectedTriangles,
            OutputHealthStatus: diagnostics.OutputHealthStatus,
            OutputHealthScore: diagnostics.OutputHealthScore,
            PreviewDecision: diagnostics.PreviewDecision,
            BudgetPressure: (InteractiveBudgetPressure)diagnostics.BudgetPressure);
    }
}
