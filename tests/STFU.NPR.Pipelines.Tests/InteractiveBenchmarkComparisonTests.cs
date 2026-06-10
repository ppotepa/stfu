using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveBenchmarkComparisonTests
{
    [Fact]
    public void Comparison_report_calculates_speedup_and_interactive_ratios()
    {
        var reference = new InteractiveFrameBenchmarkReport(new[]
        {
            Sample(frame: 1, stageMs: 24, returnedInteractive: false, returnedFallback: true),
            Sample(frame: 2, stageMs: 20, returnedInteractive: false, returnedFallback: true)
        });
        var interactive = new InteractiveFrameBenchmarkReport(new[]
        {
            Sample(frame: 1, stageMs: 12, returnedInteractive: true, returnedFallback: false),
            Sample(frame: 2, stageMs: 10, returnedInteractive: true, returnedFallback: false)
        });

        var comparison = InteractiveBenchmarkComparisonReporter.BuildComparison(reference, interactive);

        Assert.Equal(22, comparison.ReferenceAverageStageMs, precision: 3);
        Assert.Equal(11, comparison.InteractiveAverageStageMs, precision: 3);
        Assert.Equal(2, comparison.SpeedupRatio, precision: 3);
        Assert.Equal(1, comparison.InteractiveReturnRatio, precision: 3);
        Assert.Equal(0, comparison.ReferenceFallbackRatio, precision: 3);
    }

    [Fact]
    public void Comparison_status_fails_when_interactive_is_slower_than_reference()
    {
        var reference = new InteractiveFrameBenchmarkReport(new[]
        {
            Sample(frame: 1, stageMs: 10, returnedInteractive: false, returnedFallback: true),
            Sample(frame: 2, stageMs: 10, returnedInteractive: false, returnedFallback: true)
        });
        var interactive = new InteractiveFrameBenchmarkReport(new[]
        {
            Sample(frame: 1, stageMs: 20, returnedInteractive: true, returnedFallback: false),
            Sample(frame: 2, stageMs: 20, returnedInteractive: true, returnedFallback: false)
        });

        var thresholds = new InteractiveBenchmarkComparisonThresholds(MaximumStageRatio: 1.20d);
        var comparison = new InteractiveBenchmarkComparisonReport(reference, interactive, thresholds);

        Assert.Equal(InteractiveBenchmarkComparisonStatus.Fail, comparison.Status);
    }

    [Fact]
    public void Comparison_report_writes_summary_and_csv()
    {
        var reference = new InteractiveFrameBenchmarkReport(new[]
        {
            Sample(frame: 1, stageMs: 18, returnedInteractive: false, returnedFallback: true)
        });
        var interactive = new InteractiveFrameBenchmarkReport(new[]
        {
            Sample(frame: 1, stageMs: 9, returnedInteractive: true, returnedFallback: false)
        });

        var comparison = InteractiveBenchmarkComparisonReporter.BuildComparison(reference, interactive);
        var summary = InteractiveBenchmarkComparisonReporter.WriteSummary(comparison);
        var csv = InteractiveBenchmarkComparisonReporter.WriteCsv(comparison);

        Assert.Contains("speedup_ratio", summary);
        Assert.Contains("referenceFallbackRatio", csv);
        Assert.Contains("2", csv);
    }

    [Fact]
    public void Default_benchmark_suite_has_stable_resolution_labels()
    {
        Assert.Contains(InteractiveBenchmarkScenario.DefaultSuite, scenario => scenario.ResolutionLabel == "320x240");
        Assert.Contains(InteractiveBenchmarkScenario.DefaultSuite, scenario => scenario.AssetPath == "assets/walking.fbx");
    }

    private static InteractiveFrameBenchmarkSample Sample(
        long frame,
        double stageMs,
        bool returnedInteractive,
        bool returnedFallback)
    {
        return new InteractiveFrameBenchmarkSample(
            FrameId: frame,
            QualityMode: InteractiveQualityMode.BalancedViewport,
            WorkClass: InteractiveWorkClass.FullVisibleStrokeRefresh,
            TotalStageMs: stageMs,
            ProjectionMs: stageMs * 0.20d,
            VisibilityMs: stageMs * 0.20d,
            CandidateMs: stageMs * 0.20d,
            StrokePlanMs: stageMs * 0.20d,
            TonePlanMs: stageMs * 0.20d,
            ProjectedTriangles: 100,
            VisibleFaces: 80,
            CandidateEdges: 70,
            StrokeCommands: 60,
            VisibleStrokeSegments: 50,
            ToneRegions: 10,
            ReturnedInteractiveFrame: returnedInteractive,
            ReturnedReferenceFallback: returnedFallback,
            ProjectionBuiltSelfContained: returnedInteractive,
            CandidateEdgesBuiltFromProjectedTriangles: returnedInteractive,
            OutputHealthStatus: returnedInteractive
                ? InteractiveOutputHealthStatus.ReturningInteractivePreview
                : InteractiveOutputHealthStatus.ReturningReferenceFallback,
            OutputHealthScore: returnedInteractive ? 90 : 85,
            PreviewDecision: InteractivePreviewDecisionKind.PreviewReady,
            BudgetPressure: InteractiveBudgetPressure.Stable);
    }
}
