using STFU.NPR.Pipeline;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveDiagnosticsBridge
{
    public static void WriteToContext(NprContext context, InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnostics);

        HarvestReferenceGraph(context, diagnostics);

        context.Counters.Set("InteractivePerformance.frameId", diagnostics.FrameId);
        context.Counters.Set("InteractivePerformance.workClass", (long)diagnostics.WorkClass);
        context.Counters.Set("InteractivePerformance.qualityMode", (long)diagnostics.QualityMode);
        context.Counters.Set("InteractivePerformance.requestedQualityMode", (long)diagnostics.RequestedQualityMode);
        context.Counters.Set("InteractivePerformance.resolvedQualityMode", (long)diagnostics.ResolvedQualityMode);
        context.Counters.Set("InteractivePerformance.budgetPressure", diagnostics.BudgetPressure);
        context.Counters.Set("InteractivePerformance.previousKnownFrameMs", (long)Math.Round(diagnostics.PreviousKnownFrameMs));
        context.Counters.Set("InteractivePerformance.targetFrameMs", (long)Math.Round(diagnostics.TargetFrameMs));
        context.Counters.Set("InteractivePerformance.overBudgetStreak", diagnostics.OverBudgetStreak);
        context.Counters.Set("InteractivePerformance.underBudgetStreak", diagnostics.UnderBudgetStreak);
        context.Counters.Set("InteractivePerformance.budgetQualityChanged", diagnostics.BudgetQualityChanged ? 1 : 0);
        context.Counters.Set("InteractivePerformance.effectiveMaxCandidateEdges", diagnostics.EffectiveMaxCandidateEdges);
        context.Counters.Set("InteractivePerformance.effectiveMaxStrokeCommands", diagnostics.EffectiveMaxStrokeCommands);
        context.Counters.Set("InteractivePerformance.effectiveMaxVisibleStrokeSegments", diagnostics.EffectiveMaxVisibleStrokeSegments);
        context.Counters.Set("InteractivePerformance.effectiveToneDeferred", diagnostics.EffectiveToneDeferred ? 1 : 0);
        context.Counters.Set("InteractivePerformance.cameraChanged", diagnostics.CameraChanged ? 1 : 0);
        context.Counters.Set("InteractivePerformance.sceneChanged", diagnostics.SceneChanged ? 1 : 0);
        context.Counters.Set("InteractivePerformance.styleChanged", diagnostics.StyleChanged ? 1 : 0);
        context.Counters.Set("InteractivePerformance.viewportSizeChanged", diagnostics.ViewportSizeChanged ? 1 : 0);
        context.Counters.Set("InteractivePerformance.debugOverlayChanged", diagnostics.DebugOverlayChanged ? 1 : 0);
        context.Counters.Set("InteractivePerformance.contentHash", ToCounterValue(diagnostics.ContentHash));
        context.Counters.Set("InteractivePerformance.cameraHash", ToCounterValue(diagnostics.CameraHash));
        context.Counters.Set("InteractivePerformance.styleHash", ToCounterValue(diagnostics.StyleHash));
        context.Counters.Set("InteractivePerformance.viewportHash", ToCounterValue(diagnostics.ViewportHash));
        context.Counters.Set("InteractivePerformance.artifactStoreItems", diagnostics.ArtifactStoreItemCount);
        context.Counters.Set("InteractivePerformance.frameOrCameraArtifacts", diagnostics.FrameOrCameraArtifactCount);
        context.Counters.Set("InteractivePerformance.prunedFrameOrCameraArtifacts", diagnostics.PrunedFrameOrCameraArtifactCount);
        context.Counters.Set("InteractivePerformance.projectionSource", diagnostics.ProjectionSource);
        context.Counters.Set("InteractivePerformance.projectionBuiltSelfContained", diagnostics.ProjectionBuiltSelfContained ? 1 : 0);
        context.Counters.Set("InteractivePerformance.projectionSourceEntities", diagnostics.ProjectionSourceEntities);
        context.Counters.Set("InteractivePerformance.projectionMeshes", diagnostics.ProjectionMeshes);
        context.Counters.Set("InteractivePerformance.projectedVertices", diagnostics.ProjectedVertices);
        context.Counters.Set("InteractivePerformance.projectedTriangles", diagnostics.ProjectedTriangles);
        context.Counters.Set("InteractivePerformance.visibleProjectedVertices", diagnostics.VisibleProjectedVertices);
        context.Counters.Set("InteractivePerformance.visibleProjectedTriangles", diagnostics.VisibleProjectedTriangles);
        context.Counters.Set("InteractivePerformance.frontFacingProjectedTriangles", diagnostics.FrontFacingProjectedTriangles);

        context.Counters.Set("InteractivePerformance.visibilitySource", diagnostics.VisibilitySource);
        context.Counters.Set("InteractivePerformance.visibilityUsedProjectedTriangles", diagnostics.VisibilityUsedProjectedTriangles ? 1 : 0);
        context.Counters.Set("InteractivePerformance.visibilitySourceProjectedTriangles", diagnostics.VisibilitySourceProjectedTriangles);
        context.Counters.Set("InteractivePerformance.totalFaces", diagnostics.TotalFaces);
        context.Counters.Set("InteractivePerformance.visibleFaces", diagnostics.VisibleFaces);
        context.Counters.Set("InteractivePerformance.visibleFaceRatioPercent", (long)Math.Round(diagnostics.VisibleFaceRatioPercent));
        context.Counters.Set("InteractivePerformance.totalEdges", diagnostics.TotalEdges);
        context.Counters.Set("InteractivePerformance.candidateEdges", diagnostics.CandidateEdges);
        context.Counters.Set("InteractivePerformance.candidateReductionPercent", (long)Math.Round(diagnostics.CandidateReductionPercent));
        context.Counters.Set("InteractivePerformance.candidateEdgeSource", diagnostics.CandidateEdgeSource);
        context.Counters.Set("InteractivePerformance.candidateEdgesBuiltFromProjectedTriangles", diagnostics.CandidateEdgesBuiltFromProjectedTriangles ? 1 : 0);
        context.Counters.Set("InteractivePerformance.candidateEdgeSourceReferenceFragments", diagnostics.CandidateEdgeSourceReferenceFragments);
        context.Counters.Set("InteractivePerformance.candidateEdgeSourceProjectedTriangles", diagnostics.CandidateEdgeSourceProjectedTriangles);
        context.Counters.Set("InteractivePerformance.candidateEdgesBeforeBudget", diagnostics.CandidateEdgesBeforeBudget);
        context.Counters.Set("InteractivePerformance.candidateEdgesAfterBudget", diagnostics.CandidateEdgesAfterBudget);
        context.Counters.Set("InteractivePerformance.candidateEdgeBudgetApplied", diagnostics.CandidateEdgeBudgetApplied ? 1 : 0);
        context.Counters.Set("InteractivePerformance.totalStrokeCandidates", diagnostics.TotalStrokeCandidates);
        context.Counters.Set("InteractivePerformance.strokeCommands", diagnostics.StrokeCommands);
        context.Counters.Set("InteractivePerformance.strokeCommandReductionPercent", (long)Math.Round(diagnostics.StrokeCommandReductionPercent));
        context.Counters.Set("InteractivePerformance.strokeCommandsBeforeBudget", diagnostics.StrokeCommandsBeforeBudget);
        context.Counters.Set("InteractivePerformance.strokeCommandsAfterBudget", diagnostics.StrokeCommandsAfterBudget);
        context.Counters.Set("InteractivePerformance.strokeCommandBudgetApplied", diagnostics.StrokeCommandBudgetApplied ? 1 : 0);
        context.Counters.Set("InteractivePerformance.visibleSegments", diagnostics.VisibleSegments);
        context.Counters.Set("InteractivePerformance.visibleSegmentSourceCommands", diagnostics.VisibleSegmentSourceCommands);
        context.Counters.Set("InteractivePerformance.visibleSegmentCoveragePercent", (long)Math.Round(diagnostics.VisibleSegmentCoveragePercent));
        context.Counters.Set("InteractivePerformance.visibleSegmentsBeforeBudget", diagnostics.VisibleSegmentsBeforeBudget);
        context.Counters.Set("InteractivePerformance.visibleSegmentsAfterBudget", diagnostics.VisibleSegmentsAfterBudget);
        context.Counters.Set("InteractivePerformance.visibleSegmentBudgetApplied", diagnostics.VisibleSegmentBudgetApplied ? 1 : 0);
        context.Counters.Set("InteractivePerformance.interactiveStrokeFrameSourceSegments", diagnostics.InteractiveStrokeFrameSourceSegments);
        context.Counters.Set("InteractivePerformance.interactiveStrokeFramePaths", diagnostics.InteractiveStrokeFramePaths);
        context.Counters.Set("InteractivePerformance.interactiveStrokeFrameSegments", diagnostics.InteractiveStrokeFrameSegments);
        context.Counters.Set("InteractivePerformance.interactiveStrokeFrameCoveragePercent", (long)Math.Round(diagnostics.InteractiveStrokeFrameCoveragePercent));
        context.Counters.Set("InteractivePerformance.tonePlanningDeferred", diagnostics.TonePlanningDeferred ? 1 : 0);
        context.Counters.Set("InteractivePerformance.toneSourceFaces", diagnostics.ToneSourceFaces);
        context.Counters.Set("InteractivePerformance.toneRegions", diagnostics.ToneRegions);
        context.Counters.Set("InteractivePerformance.toneCoverageRatioPercent", (long)Math.Round(diagnostics.ToneCoverageRatioPercent));
        context.Counters.Set("InteractivePerformance.toneHighlightRegions", diagnostics.ToneHighlightRegions);
        context.Counters.Set("InteractivePerformance.toneMidtoneRegions", diagnostics.ToneMidtoneRegions);
        context.Counters.Set("InteractivePerformance.toneShadowRegions", diagnostics.ToneShadowRegions);
        context.Counters.Set("InteractivePerformance.outputKind", (long)diagnostics.OutputKind);
        context.Counters.Set("InteractivePerformance.outputReadiness", (long)diagnostics.OutputReadiness);
        context.Counters.Set("InteractivePerformance.outputReadinessScore", diagnostics.OutputReadinessScore);
        context.Counters.Set("InteractivePerformance.interactivePreviewCandidate", diagnostics.InteractivePreviewCandidateAvailable ? 1 : 0);
        context.Counters.Set("InteractivePerformance.outputProjectedVertices", diagnostics.OutputProjectedVertices);
        context.Counters.Set("InteractivePerformance.outputProjectedTriangles", diagnostics.OutputProjectedTriangles);
        context.Counters.Set("InteractivePerformance.outputVisibleFaces", diagnostics.OutputVisibleFaces);
        context.Counters.Set("InteractivePerformance.outputCandidateEdges", diagnostics.OutputCandidateEdges);
        context.Counters.Set("InteractivePerformance.outputStrokeCommands", diagnostics.OutputStrokeCommands);
        context.Counters.Set("InteractivePerformance.outputVisibleStrokeSegments", diagnostics.OutputVisibleStrokeSegments);
        context.Counters.Set("InteractivePerformance.outputInteractiveStrokeFramePaths", diagnostics.OutputInteractiveStrokeFramePaths);
        context.Counters.Set("InteractivePerformance.outputInteractiveStrokeFrameSegments", diagnostics.OutputInteractiveStrokeFrameSegments);
        context.Counters.Set("InteractivePerformance.outputToneRegions", diagnostics.OutputToneRegions);
        context.Counters.Set("InteractivePerformance.previewDecision", (long)diagnostics.PreviewDecision);
        context.Counters.Set("InteractivePerformance.previewCandidateReadinessScore", diagnostics.PreviewCandidateReadinessScore);
        context.Counters.Set("InteractivePerformance.previewMinimumReadinessScore", diagnostics.PreviewMinimumReadinessScore);
        context.Counters.Set("InteractivePerformance.previewRejectedByReadinessGate", diagnostics.PreviewRejectedByReadinessGate ? 1 : 0);
        context.Counters.Set("InteractivePerformance.previewRejectedBySegmentBudget", diagnostics.PreviewRejectedBySegmentBudget ? 1 : 0);
        context.Counters.Set("InteractivePerformance.returnedInteractiveFrame", diagnostics.ReturnedInteractiveFrame ? 1 : 0);
        context.Counters.Set("InteractivePerformance.returnedReferenceFallback", diagnostics.ReturnedReferenceFallback ? 1 : 0);
        context.Counters.Set("InteractivePerformance.returnedInteractiveFramePaths", diagnostics.ReturnedInteractiveFramePaths);
        context.Counters.Set("InteractivePerformance.returnedInteractiveFrameSegments", diagnostics.ReturnedInteractiveFrameSegments);
        context.Counters.Set("InteractivePerformance.outputHealthStatus", (long)diagnostics.OutputHealthStatus);
        context.Counters.Set("InteractivePerformance.outputHealthScore", diagnostics.OutputHealthScore);
        context.Counters.Set("InteractivePerformance.outputHealthWarningCount", diagnostics.OutputHealthWarningCount);
        context.Counters.Set("InteractivePerformance.referenceExecutionMode", (long)diagnostics.ReferenceExecutionMode);
        context.Counters.Set("InteractivePerformance.referenceExecutedBeforeInteractive", diagnostics.ReferenceExecutedBeforeInteractive ? 1 : 0);
        context.Counters.Set("InteractivePerformance.referenceExecutedAfterInteractive", diagnostics.ReferenceExecutedAfterInteractive ? 1 : 0);
        context.Counters.Set("InteractivePerformance.referenceExecutionSkipped", diagnostics.ReferenceExecutionSkipped ? 1 : 0);
        context.Counters.Set("InteractivePerformance.referenceExecutionDisabledForPreview", diagnostics.ReferenceExecutionDisabledForPreview ? 1 : 0);
        context.Counters.Set("InteractivePerformance.referenceFallbackFrameAvailable", diagnostics.ReferenceFallbackFrameAvailable ? 1 : 0);
        context.Counters.Set("InteractivePerformance.referenceFallbackUnavailable", diagnostics.ReferenceFallbackUnavailable ? 1 : 0);
        context.Counters.Set("InteractivePerformance.referenceFallbackEmptyFrame", diagnostics.ReferenceFallbackEmptyFrame ? 1 : 0);
        context.Counters.Set("InteractivePerformance.cacheHits", diagnostics.CacheHits);
        context.Counters.Set("InteractivePerformance.cacheMisses", diagnostics.CacheMisses);
        context.Counters.Set("InteractivePerformance.usedReferenceFallback", diagnostics.UsedReferenceFallback ? 1 : 0);
    }

    private static long ToCounterValue(ulong value)
    {
        return unchecked((long)value);
    }

    private static void HarvestReferenceGraph(NprContext context, InteractiveFrameDiagnostics diagnostics)
    {
        var graph = context.Graph;
        var totalFaces = graph.Triangles.Count;
        var visibleFaces = CountVisibleFaces(graph.DefaultFaceIdVisibility?.FaceVisible, totalFaces);
        var totalEdges = graph.DefaultFragments.Count > 0
            ? graph.DefaultFragments.Count
            : graph.TopologyEdges.Count;
        var candidateEdges = CountCandidateEdgesForVisibleFaces(graph.DefaultFragments, graph.DefaultFaceIdVisibility?.FaceVisible);

        if (diagnostics.VisibilitySource == (long)InteractiveVisibilitySource.None)
        {
            diagnostics.TotalFaces = totalFaces;
            diagnostics.VisibleFaces = visibleFaces;
            diagnostics.VisibleFaceRatioPercent = totalFaces <= 0 ? 0d : visibleFaces * 100d / totalFaces;
            diagnostics.VisibilitySource = (long)InteractiveVisibilitySource.ReferenceFaceIdBuffer;
        }

        if (diagnostics.TotalEdges <= 0 && diagnostics.CandidateEdges <= 0)
        {
            diagnostics.TotalEdges = totalEdges;
            diagnostics.CandidateEdges = candidateEdges;
            diagnostics.CandidateReductionPercent = totalEdges <= 0 ? 0d : (totalEdges - candidateEdges) * 100d / totalEdges;
        }
    }

    private static int CountVisibleFaces(bool[]? faceVisible, int totalFaces)
    {
        if (totalFaces <= 0)
        {
            return 0;
        }

        if (faceVisible is null || faceVisible.Length == 0)
        {
            return totalFaces;
        }

        var count = 0;
        var limit = Math.Min(totalFaces, faceVisible.Length);
        for (var i = 0; i < limit; i++)
        {
            if (faceVisible[i])
            {
                count++;
            }
        }

        return count;
    }

    private static int CountCandidateEdgesForVisibleFaces(
        IReadOnlyList<DefaultLineFragment> fragments,
        bool[]? faceVisible)
    {
        if (fragments.Count == 0)
        {
            return 0;
        }

        if (faceVisible is null || faceVisible.Length == 0)
        {
            return fragments.Count;
        }

        var count = 0;
        foreach (var fragment in fragments)
        {
            if (IsVisibleFace(fragment.FirstTriangleIndex, faceVisible) ||
                IsVisibleFace(fragment.SecondTriangleIndex, faceVisible))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsVisibleFace(int face, bool[] faceVisible)
    {
        return face >= 0 && face < faceVisible.Length && faceVisible[face];
    }
}
