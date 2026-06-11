namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveRuntimeEvidenceBuilder
{
    public static InteractiveEvidenceReport BuildFrameEvidence(
        string scenario,
        InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var bag = BuildFrameEvidenceBag(scenario, diagnostics);
        return InteractiveEvidenceReporter.Build(NormalizeScenario(scenario), bag);
    }

    public static InteractiveEvidenceBag BuildFrameEvidenceBag(
        string scenario,
        InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var bag = new InteractiveEvidenceBag();
        var normalizedScenario = NormalizeScenario(scenario);

        AddText(bag, "scenario", normalizedScenario, InteractiveEvidenceKind.Scenario);
        AddText(bag, "strategy", diagnostics.Strategy.ToString(), InteractiveEvidenceKind.Scenario);
        AddText(bag, "workClass", diagnostics.WorkClass.ToString(), InteractiveEvidenceKind.Scenario);

        AddBool(
            bag,
            "preview.returnedInteractiveFrame",
            diagnostics.ReturnedInteractiveFrame,
            InteractiveEvidenceKind.PreviewDecision,
            InteractiveEvidenceSeverity.Warning);
        AddBool(
            bag,
            "preview.returnedReferenceFallback",
            diagnostics.ReturnedReferenceFallback,
            InteractiveEvidenceKind.PreviewDecision,
            InteractiveEvidenceSeverity.Warning);
        AddText(bag, "preview.decision", diagnostics.PreviewDecision.ToString(), InteractiveEvidenceKind.PreviewDecision);
        AddNumber(bag, "preview.candidateReadinessScore", diagnostics.PreviewCandidateReadinessScore, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "preview.minimumReadinessScore", diagnostics.PreviewMinimumReadinessScore, InteractiveEvidenceKind.Metric);
        AddBool(
            bag,
            "preview.rejectedByReadinessGate",
            diagnostics.PreviewRejectedByReadinessGate,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddBool(
            bag,
            "preview.rejectedBySegmentBudget",
            diagnostics.PreviewRejectedBySegmentBudget,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);

        AddBool(
            bag,
            "projection.selfContained",
            diagnostics.ProjectionBuiltSelfContained,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddBool(
            bag,
            "projection.usedReferenceGraph",
            diagnostics.ProjectionUsedReferenceGraph,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddNumber(bag, "projection.inputMeshes", diagnostics.ProjectionInputMeshCount, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "projection.inputVertices", diagnostics.ProjectionInputVertexCount, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "projection.inputTriangles", diagnostics.ProjectionInputTriangleCount, InteractiveEvidenceKind.Metric);
        AddText(bag, "projection.inputSourceNote", diagnostics.ProjectionInputSourceNote, InteractiveEvidenceKind.Scenario);
        AddText(bag, "projection.source", diagnostics.ProjectionSource.ToString(), InteractiveEvidenceKind.Metric);
        AddNumber(bag, "projection.sourceEntities", diagnostics.ProjectionSourceEntities, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "projection.meshes", diagnostics.ProjectionMeshes, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "projection.vertices", diagnostics.ProjectedVertices, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "projection.triangles", diagnostics.ProjectedTriangles, InteractiveEvidenceKind.Metric);

        AddText(bag, "visibility.source", diagnostics.VisibilitySource.ToString(), InteractiveEvidenceKind.Metric);
        AddText(bag, "visibility.provider", diagnostics.VisibilityProviderName, InteractiveEvidenceKind.Scenario);
        AddNumber(bag, "visibility.sourceProjectedTriangles", diagnostics.VisibilitySourceProjectedTriangles, InteractiveEvidenceKind.Metric);
        AddBool(
            bag,
            "visibility.usedProjectedTriangles",
            diagnostics.VisibilityUsedProjectedTriangles,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddNumber(bag, "visibility.totalFaces", diagnostics.TotalFaces, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "visibility.visibleFaces", diagnostics.VisibleFaces, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "visibility.visibleFaceRatioPercent", diagnostics.VisibleFaceRatioPercent, InteractiveEvidenceKind.Metric);

        AddText(bag, "candidate.source", diagnostics.CandidateEdgeSource.ToString(), InteractiveEvidenceKind.Scenario);
        AddNumber(bag, "candidate.referenceFragments", diagnostics.CandidateEdgeSourceReferenceFragments, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "candidate.projectedTriangles", diagnostics.CandidateEdgeSourceProjectedTriangles, InteractiveEvidenceKind.Metric);
        AddBool(
            bag,
            "candidate.builtFromProjectedTriangles",
            diagnostics.CandidateEdgesBuiltFromProjectedTriangles,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddNumber(bag, "candidate.beforeBudget", diagnostics.CandidateEdgesBeforeBudget, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "candidate.afterBudget", diagnostics.CandidateEdgesAfterBudget, InteractiveEvidenceKind.Metric);
        AddBool(
            bag,
            "candidate.budgetApplied",
            diagnostics.CandidateEdgeBudgetApplied,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);

        AddNumber(bag, "stroke.totalCandidates", diagnostics.TotalStrokeCandidates, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "stroke.commands", diagnostics.StrokeCommands, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "stroke.beforeBudget", diagnostics.StrokeCommandsBeforeBudget, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "stroke.afterBudget", diagnostics.StrokeCommandsAfterBudget, InteractiveEvidenceKind.Metric);
        AddBool(
            bag,
            "stroke.budgetApplied",
            diagnostics.StrokeCommandBudgetApplied,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);

        AddNumber(bag, "visibleSegments.total", diagnostics.VisibleSegments, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "visibleSegments.sourceCommands", diagnostics.VisibleSegmentSourceCommands, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "visibleSegments.beforeBudget", diagnostics.VisibleSegmentsBeforeBudget, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "visibleSegments.afterBudget", diagnostics.VisibleSegmentsAfterBudget, InteractiveEvidenceKind.Metric);
        AddBool(
            bag,
            "visibleSegments.budgetApplied",
            diagnostics.VisibleSegmentBudgetApplied,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);

        AddNumber(bag, "interactiveStrokeFrame.sourceSegments", diagnostics.InteractiveStrokeFrameSourceSegments, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "interactiveStrokeFrame.paths", diagnostics.InteractiveStrokeFramePaths, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "interactiveStrokeFrame.segments", diagnostics.InteractiveStrokeFrameSegments, InteractiveEvidenceKind.Metric);

        AddBool(
            bag,
            "tone.deferred",
            diagnostics.TonePlanningDeferred,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddNumber(bag, "tone.sourceFaces", diagnostics.ToneSourceFaces, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "tone.regions", diagnostics.ToneRegions, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "tone.coverageRatioPercent", diagnostics.ToneCoverageRatioPercent, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "tone.highlightRegions", diagnostics.ToneHighlightRegions, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "tone.midtoneRegions", diagnostics.ToneMidtoneRegions, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "tone.shadowRegions", diagnostics.ToneShadowRegions, InteractiveEvidenceKind.Metric);

        AddText(bag, "output.kind", diagnostics.OutputKind.ToString(), InteractiveEvidenceKind.Gate);
        AddBool(
            bag,
            "output.previewCandidateAvailable",
            diagnostics.InteractivePreviewCandidateAvailable,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddText(bag, "output.readiness", diagnostics.OutputReadiness.ToString(), InteractiveEvidenceKind.Gate);
        AddNumber(bag, "output.readinessScore", diagnostics.OutputReadinessScore, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.projectedVertices", diagnostics.OutputProjectedVertices, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.projectedTriangles", diagnostics.OutputProjectedTriangles, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.visibleFaces", diagnostics.OutputVisibleFaces, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.candidateEdges", diagnostics.OutputCandidateEdges, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.strokeCommands", diagnostics.OutputStrokeCommands, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.visibleStrokeSegments", diagnostics.OutputVisibleStrokeSegments, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.interactiveStrokeFramePaths", diagnostics.OutputInteractiveStrokeFramePaths, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.interactiveStrokeFrameSegments", diagnostics.OutputInteractiveStrokeFrameSegments, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.toneRegions", diagnostics.OutputToneRegions, InteractiveEvidenceKind.Metric);
        AddText(bag, "output.reason", diagnostics.OutputReason, InteractiveEvidenceKind.Gate);

        AddText(bag, "reference.executionMode", diagnostics.ReferenceExecutionMode.ToString(), InteractiveEvidenceKind.Scenario);
        AddBool(
            bag,
            "reference.executedBeforeInteractive",
            diagnostics.ReferenceExecutedBeforeInteractive,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Info);
        AddBool(
            bag,
            "reference.executedAfterInteractive",
            diagnostics.ReferenceExecutedAfterInteractive,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Info);
        AddBool(
            bag,
            "reference.executionSkipped",
            diagnostics.ReferenceExecutionSkipped,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddBool(
            bag,
            "reference.disabledForPreview",
            diagnostics.ReferenceExecutionDisabledForPreview,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddBool(
            bag,
            "reference.fallbackFrameAvailable",
            diagnostics.ReferenceFallbackFrameAvailable,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Info);
        AddBool(
            bag,
            "reference.fallbackUnavailable",
            diagnostics.ReferenceFallbackUnavailable,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddBool(
            bag,
            "reference.fallbackEmptyFrame",
            diagnostics.ReferenceFallbackEmptyFrame,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddText(bag, "reference.reason", diagnostics.ReferenceExecutionReason, InteractiveEvidenceKind.Gate);

        AddNumber(bag, "timing.totalInteractiveStageMs", diagnostics.TotalInteractiveStageMs, InteractiveEvidenceKind.StageTiming);
        AddNumber(bag, "timing.projectionMs", diagnostics.ProjectionMs, InteractiveEvidenceKind.StageTiming);
        AddNumber(bag, "timing.visibilityMs", diagnostics.VisibilityMs, InteractiveEvidenceKind.StageTiming);
        AddNumber(bag, "timing.candidateMs", diagnostics.CandidateMs, InteractiveEvidenceKind.StageTiming);
        AddNumber(bag, "timing.strokePlanMs", diagnostics.StrokePlanMs, InteractiveEvidenceKind.StageTiming);
        AddNumber(bag, "timing.tonePlanMs", diagnostics.TonePlanMs, InteractiveEvidenceKind.StageTiming);
        AddNumber(bag, "timing.gpuUploadMs", diagnostics.GpuUploadMs, InteractiveEvidenceKind.StageTiming);
        AddNumber(bag, "timing.gpuDrawMs", diagnostics.GpuDrawMs, InteractiveEvidenceKind.StageTiming);

        AddNumber(bag, "output.healthScore", diagnostics.OutputHealthScore, InteractiveEvidenceKind.Metric);
        AddNumber(bag, "output.healthWarnings", diagnostics.OutputHealthWarningCount, InteractiveEvidenceKind.Metric);
        AddText(bag, "output.healthStatus", diagnostics.OutputHealthStatus.ToString(), InteractiveEvidenceKind.Gate, SeverityForHealth(diagnostics.OutputHealthStatus));
        AddText(bag, "output.healthSummary", diagnostics.OutputHealthSummary, InteractiveEvidenceKind.Gate);
        AddBool(
            bag,
            "runtime.usedReferenceFallback",
            diagnostics.UsedReferenceFallback,
            InteractiveEvidenceKind.Gate,
            InteractiveEvidenceSeverity.Warning);
        AddText(bag, "runtime.fallbackReason", diagnostics.FallbackReason, InteractiveEvidenceKind.Gate);

        return bag;
    }

    public static InteractivePerformanceRunSummary BuildInteractiveSummary(
        string scenario,
        InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new InteractivePerformanceRunSummary
        {
            Strategy = "InteractivePerformance",
            Scenario = NormalizeScenario(scenario),
            FrameCount = 1,
            AverageTotalMs = diagnostics.TotalInteractiveStageMs,
            P95TotalMs = diagnostics.TotalInteractiveStageMs,
            AverageProjectionMs = diagnostics.ProjectionMs,
            AverageVisibilityMs = diagnostics.VisibilityMs,
            AverageCandidateMs = diagnostics.CandidateMs,
            AverageStrokeMs = diagnostics.StrokePlanMs,
            AverageToneMs = diagnostics.TonePlanMs,
            InteractiveReturnRatio = diagnostics.ReturnedInteractiveFrame ? 1d : 0d,
            ReferenceFallbackRatio = diagnostics.ReturnedReferenceFallback ? 1d : 0d,
            SelfContainedProjectionRatio = diagnostics.ProjectionBuiltSelfContained ? 1d : 0d,
            ProjectedTriangleCandidateRatio = diagnostics.CandidateEdgesBuiltFromProjectedTriangles ? 1d : 0d,
            AverageHealthScore = diagnostics.OutputHealthScore
        };
    }

    public static InteractivePerformanceRunSummary BuildReferenceBaseline(
        string scenario,
        double referenceAverageMs,
        double healthScore = 100d)
    {
        return new InteractivePerformanceRunSummary
        {
            Strategy = "ReferenceQuality",
            Scenario = NormalizeScenario(scenario),
            FrameCount = 1,
            AverageTotalMs = referenceAverageMs,
            P95TotalMs = referenceAverageMs,
            AverageHealthScore = healthScore
        };
    }

    public static InteractiveRunComparisonSnapshot BuildComparison(
        string scenario,
        double referenceAverageMs,
        InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var reference = BuildReferenceBaseline(scenario, referenceAverageMs);
        var interactive = BuildInteractiveSummary(scenario, diagnostics);
        return InteractiveRunComparisonSnapshotBuilder.Build(NormalizeScenario(scenario), reference, interactive);
    }

    private static void AddText(
        InteractiveEvidenceBag bag,
        string key,
        string value,
        InteractiveEvidenceKind kind,
        InteractiveEvidenceSeverity severity = InteractiveEvidenceSeverity.Info)
    {
        bag.Add(key, value ?? string.Empty, kind, severity);
    }

    private static void AddBool(
        InteractiveEvidenceBag bag,
        string key,
        bool value,
        InteractiveEvidenceKind kind,
        InteractiveEvidenceSeverity falseSeverity)
    {
        bag.Add(key, value ? "true" : "false", kind, value ? InteractiveEvidenceSeverity.Info : falseSeverity);
    }

    private static void AddNumber(
        InteractiveEvidenceBag bag,
        string key,
        int value,
        InteractiveEvidenceKind kind)
    {
        bag.Add(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture), kind, InteractiveEvidenceSeverity.Info);
    }

    private static void AddNumber(
        InteractiveEvidenceBag bag,
        string key,
        double value,
        InteractiveEvidenceKind kind)
    {
        bag.Add(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture), kind, InteractiveEvidenceSeverity.Info);
    }

    private static InteractiveEvidenceSeverity SeverityForHealth(InteractiveOutputHealthStatus status)
    {
        return status switch
        {
            InteractiveOutputHealthStatus.ReturningInteractivePreview => InteractiveEvidenceSeverity.Info,
            InteractiveOutputHealthStatus.PreviewCandidateReady => InteractiveEvidenceSeverity.Info,
            InteractiveOutputHealthStatus.StrokeDataReady => InteractiveEvidenceSeverity.Info,
            InteractiveOutputHealthStatus.VisibleGeometry => InteractiveEvidenceSeverity.Info,
            InteractiveOutputHealthStatus.ReturningReferenceFallback => InteractiveEvidenceSeverity.Warning,
            InteractiveOutputHealthStatus.ProjectionOnly => InteractiveEvidenceSeverity.Warning,
            InteractiveOutputHealthStatus.NoInteractiveArtifacts => InteractiveEvidenceSeverity.Warning,
            _ => InteractiveEvidenceSeverity.Warning
        };
    }

    private static string NormalizeScenario(string scenario)
    {
        return string.IsNullOrWhiteSpace(scenario)
            ? "interactive-frame"
            : scenario.Trim();
    }
}
