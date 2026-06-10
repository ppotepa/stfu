using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveBenchmarkReportTests
{
    [Fact]
    public void Diagnostics_exposes_total_interactive_stage_ms_for_budget_and_benchmarking()
    {
        var diagnostics = new InteractiveFrameDiagnostics();

        diagnostics.AddStageTiming("InteractiveProjection", TimeSpan.FromMilliseconds(1));
        diagnostics.AddStageTiming("InteractiveVisibility", TimeSpan.FromMilliseconds(2));
        diagnostics.AddStageTiming("InteractiveCandidateEdges", TimeSpan.FromMilliseconds(3));
        diagnostics.AddStageTiming("InteractiveStrokePlanning", TimeSpan.FromMilliseconds(4));
        diagnostics.AddStageTiming("InteractiveTonePlanning", TimeSpan.FromMilliseconds(5));

        Assert.Equal(15, diagnostics.TotalInteractiveStageMs, precision: 3);
    }

    [Fact]
    public void Benchmark_report_summarizes_interactive_and_reference_fallback_ratios()
    {
        var first = CreateDiagnostics(1, stageMs: 12, returnedInteractive: true, returnedFallback: false);
        var second = CreateDiagnostics(2, stageMs: 20, returnedInteractive: false, returnedFallback: true);

        var report = InteractiveFrameBenchmarkReporter.BuildReport([first, second]);

        Assert.Equal(2, report.SampleCount);
        Assert.Equal(16, report.AverageStageMs, precision: 3);
        Assert.Equal(20, report.MaxStageMs, precision: 3);
        Assert.Equal(0.5, report.InteractiveReturnRatio, precision: 3);
        Assert.Equal(0.5, report.ReferenceFallbackRatio, precision: 3);
    }

    [Fact]
    public void Benchmark_report_writes_summary_and_csv_for_validation_artifacts()
    {
        var report = InteractiveFrameBenchmarkReporter.BuildReport([
            CreateDiagnostics(7, stageMs: 10, returnedInteractive: true, returnedFallback: false)
        ]);

        var summary = InteractiveFrameBenchmarkReporter.WriteSummary(report);
        var csv = InteractiveFrameBenchmarkReporter.WriteCsv(report);

        Assert.Contains("STFU Interactive Performance benchmark report", summary);
        Assert.Contains("avg_stage_ms", summary);
        Assert.Contains("interactive_return_ratio", summary);
        Assert.Contains("frameId,qualityMode,workClass,totalStageMs", csv);
        Assert.Contains("7,FastPreview", csv);
    }

    [Fact]
    public void Benchmark_thresholds_fail_when_reference_fallback_ratio_is_too_high()
    {
        var report = InteractiveFrameBenchmarkReporter.BuildReport([
            CreateDiagnostics(1, stageMs: 12, returnedInteractive: false, returnedFallback: true),
            CreateDiagnostics(2, stageMs: 13, returnedInteractive: false, returnedFallback: true)
        ]);
        var thresholds = new InteractiveFrameBenchmarkThresholds(MaximumReferenceFallbackRatio: 0.25);

        var status = thresholds.Evaluate(report);

        Assert.Equal(InteractiveFrameBenchmarkStatus.Fail, status);
    }

    private static InteractiveFrameDiagnostics CreateDiagnostics(
        long frameId,
        double stageMs,
        bool returnedInteractive,
        bool returnedFallback)
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            FrameId = frameId,
            QualityMode = InteractiveQualityMode.FastPreview,
            WorkClass = InteractiveWorkClass.FullVisibleStrokeRefresh,
            ProjectedTriangles = 30,
            VisibleFaces = 20,
            CandidateEdges = 12,
            StrokeCommands = 8,
            VisibleSegments = 5,
            ToneRegions = 4,
            ReturnedInteractiveFrame = returnedInteractive,
            ReturnedReferenceFallback = returnedFallback,
            ProjectionBuiltSelfContained = true,
            CandidateEdgeSource = (long)InteractiveCandidateEdgeSource.ProjectedTriangleEdges,
            OutputHealthStatus = InteractiveOutputHealthStatus.PreviewCandidateReady,
            OutputHealthScore = 90,
            PreviewDecision = InteractivePreviewDecisionKind.SelectedInteractiveFrame,
            BudgetPressure = (long)InteractiveBudgetPressure.Stable
        };

        diagnostics.AddStageTiming("InteractiveProjection", TimeSpan.FromMilliseconds(stageMs * 0.20));
        diagnostics.AddStageTiming("InteractiveVisibility", TimeSpan.FromMilliseconds(stageMs * 0.20));
        diagnostics.AddStageTiming("InteractiveCandidateEdges", TimeSpan.FromMilliseconds(stageMs * 0.20));
        diagnostics.AddStageTiming("InteractiveStrokePlanning", TimeSpan.FromMilliseconds(stageMs * 0.30));
        diagnostics.AddStageTiming("InteractiveTonePlanning", TimeSpan.FromMilliseconds(stageMs * 0.10));
        return diagnostics;
    }
}
