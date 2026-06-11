using STFU.NPR.Pipeline.InteractivePerformance.Core;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveRuntimeEvidenceIntegrationTests
{
    [Fact]
    public void Runtime_evidence_builder_exports_preview_and_timing_facts()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            ReturnedInteractiveFrame = true,
            ReturnedReferenceFallback = false,
            ProjectionBuiltSelfContained = true,
            CandidateEdgesBeforeBudget = 64,
            CandidateEdgesAfterBudget = 16,
            CandidateEdgeBudgetApplied = true,
            TotalInteractiveStageMs = 7.5,
            ProjectionMs = 2.0,
            VisibilityMs = 1.5,
            CandidateMs = 1.0,
            StrokePlanMs = 2.5,
            TonePlanMs = 0.5,
            OutputHealthScore = 95,
            OutputHealthStatus = InteractiveOutputHealthStatus.ReturningInteractivePreview
        };

        var report = InteractiveRuntimeEvidenceBuilder.BuildFrameEvidence("unit", diagnostics);

        Assert.True(report.Facts.Count > 0);
        Assert.Equal(0, report.FailureCount);
        Assert.True(report.WarningCount >= 0);
    }

    [Fact]
    public void Runtime_gate_snapshot_builder_maps_diagnostics_to_interactive_summary()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            ReturnedInteractiveFrame = true,
            ReturnedReferenceFallback = false,
            ProjectionBuiltSelfContained = true,
            TotalInteractiveStageMs = 10,
            ProjectionMs = 2,
            VisibilityMs = 3,
            CandidateMs = 1,
            StrokePlanMs = 4,
            TonePlanMs = 0.5,
            OutputHealthScore = 90
        };

        var summary = InteractiveRuntimeGateSnapshotBuilder.BuildInteractiveSummary("unit", diagnostics);

        Assert.Equal("InteractivePerformance", summary.Strategy);
        Assert.Equal("unit", summary.Scenario);
        Assert.Equal(10d, summary.AverageTotalMs);
        Assert.Equal(1d, summary.InteractiveReturnRatio);
        Assert.Equal(0d, summary.ReferenceFallbackRatio);
        Assert.Equal(1d, summary.SelfContainedProjectionRatio);
    }

    [Fact]
    public void Runtime_gate_snapshot_builder_builds_comparison_against_reference_baseline()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            ReturnedInteractiveFrame = true,
            ReturnedReferenceFallback = false,
            TotalInteractiveStageMs = 10,
            OutputHealthScore = 100
        };

        var comparison = InteractiveRuntimeGateSnapshotBuilder.BuildComparison(
            "unit",
            referenceAverageMs: 20,
            diagnostics);

        Assert.Equal("unit", comparison.Scenario);
        Assert.Equal(2d, comparison.SpeedupRatio);
        Assert.Equal(1d, comparison.InteractiveReturnRatio);
    }
}
