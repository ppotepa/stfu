using STFU.NPR.Pipeline.InteractivePerformance.Core;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractivePerformanceAnalysisSuiteTests
{
    [Fact]
    public void Metric_series_computes_average_and_percentiles()
    {
        var series = new InteractiveMetricSeries("frameMs", InteractiveMetricUnit.Milliseconds);
        series.Add(1, 10);
        series.Add(2, 20);
        series.Add(3, 30);

        var summary = series.Summarize();

        Assert.Equal(3, summary.Count);
        Assert.Equal(20, summary.Average);
        Assert.Equal(20, summary.P50);
        Assert.True(summary.P95 > 28);
    }

    [Fact]
    public void Run_aggregator_calculates_ratios()
    {
        var samples = new[]
        {
            new InteractivePerformanceRunSample { FrameId = 1, TotalMs = 10, ReturnedInteractiveFrame = true, ProjectionBuiltSelfContained = true, CandidateEdgesBuiltFromProjectedTriangles = true, HealthScore = 90 },
            new InteractivePerformanceRunSample { FrameId = 2, TotalMs = 20, ReturnedReferenceFallback = true, HealthScore = 70 }
        };

        var summary = InteractivePerformanceRunAggregator.Summarize("InteractivePerformance", "test", samples);

        Assert.Equal(2, summary.FrameCount);
        Assert.Equal(15, summary.AverageTotalMs);
        Assert.Equal(0.5, summary.InteractiveReturnRatio);
        Assert.Equal(0.5, summary.ReferenceFallbackRatio);
        Assert.Equal(80, summary.AverageHealthScore);
    }

    [Fact]
    public void Gate_evaluator_passes_fast_healthy_interactive_run()
    {
        var reference = new InteractivePerformanceRunSummary
        {
            Strategy = "ReferenceQuality",
            Scenario = "walking-preview",
            FrameCount = 4,
            AverageTotalMs = 20,
            AverageHealthScore = 90
        };
        var interactive = new InteractivePerformanceRunSummary
        {
            Strategy = "InteractivePerformance",
            Scenario = "walking-preview",
            FrameCount = 4,
            AverageTotalMs = 10,
            InteractiveReturnRatio = 1,
            ReferenceFallbackRatio = 0,
            SelfContainedProjectionRatio = 1,
            ProjectedTriangleCandidateRatio = 1,
            AverageHealthScore = 90
        };

        var result = InteractivePerformanceGateEvaluator.Evaluate(reference, interactive, new InteractivePerformanceGateThresholds());

        Assert.Equal(InteractivePerformanceGateStatus.Pass, result.Status);
        Assert.True(result.SpeedupRatio >= 2);
    }

    [Fact]
    public void Gate_evaluator_fails_high_reference_fallback_ratio()
    {
        var reference = new InteractivePerformanceRunSummary
        {
            Strategy = "ReferenceQuality",
            Scenario = "walking-preview",
            FrameCount = 4,
            AverageTotalMs = 20,
            AverageHealthScore = 90
        };
        var interactive = new InteractivePerformanceRunSummary
        {
            Strategy = "InteractivePerformance",
            Scenario = "walking-preview",
            FrameCount = 4,
            AverageTotalMs = 12,
            InteractiveReturnRatio = 0.25,
            ReferenceFallbackRatio = 0.75,
            SelfContainedProjectionRatio = 1,
            ProjectedTriangleCandidateRatio = 1,
            AverageHealthScore = 90
        };

        var result = InteractivePerformanceGateEvaluator.Evaluate(reference, interactive, new InteractivePerformanceGateThresholds());

        Assert.Equal(InteractivePerformanceGateStatus.Fail, result.Status);
        Assert.Contains(result.Failures, failure => failure.Contains("Reference fallback ratio", StringComparison.Ordinal));
    }

    [Fact]
    public void Stage_budget_planner_reports_over_budget_stages()
    {
        var plan = InteractiveStageBudgetPlanner.Plan("test", 10);
        var summary = new InteractivePerformanceRunSummary
        {
            Strategy = "InteractivePerformance",
            Scenario = "test",
            FrameCount = 1,
            AverageProjectionMs = 9
        };

        var warnings = InteractiveStageBudgetPlanner.Compare(plan, summary);

        Assert.Contains(warnings, warning => warning.Contains("Projection", StringComparison.Ordinal));
    }

    [Fact]
    public void Regression_analyzer_flags_frame_time_growth()
    {
        var baseline = new InteractivePerformanceRunSummary
        {
            Strategy = "InteractivePerformance",
            Scenario = "test",
            AverageTotalMs = 10,
            AverageHealthScore = 90
        };
        var current = baseline with
        {
            AverageTotalMs = 18,
            AverageHealthScore = 90
        };

        var report = InteractiveRegressionAnalyzer.Analyze(baseline, current, new InteractiveRegressionThresholds());

        Assert.True(report.HasErrors);
        Assert.Contains(report.Findings, finding => finding.Metric == "averageTotalMs");
    }

    [Fact]
    public void Summary_writer_outputs_gate_status_and_speedup()
    {
        var result = new InteractivePerformanceGateResult
        {
            Status = InteractivePerformanceGateStatus.Pass,
            Scenario = "test",
            ReferenceAverageMs = 20,
            InteractiveAverageMs = 10,
            SpeedupRatio = 2,
            InteractiveReturnRatio = 1,
            AverageHealthScore = 100
        };

        var text = InteractivePerformanceReportWriter.WriteSummary(result);

        Assert.Contains("Status: Pass", text, StringComparison.Ordinal);
        Assert.Contains("Speedup ratio: 2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Scenario_suite_contains_preview_balanced_quality_and_stress()
    {
        var scenarios = InteractivePerformanceScenarioSuite.DefaultViewportScenarios;

        Assert.Contains(scenarios, scenario => scenario.Name == "walking-preview");
        Assert.Contains(scenarios, scenario => scenario.Name == "walking-balanced");
        Assert.Contains(scenarios, scenario => scenario.Name == "walking-quality");
        Assert.Contains(scenarios, scenario => scenario.Name == "walking-stress");
    }
}
