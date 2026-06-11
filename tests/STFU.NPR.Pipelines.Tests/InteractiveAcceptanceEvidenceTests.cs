using STFU.NPR.Pipeline.InteractivePerformance.Core;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveAcceptanceEvidenceTests
{
    [Fact]
    public void Run_comparison_snapshot_calculates_speedup()
    {
        var reference = new InteractivePerformanceRunSummary
        {
            Strategy = "ReferenceQuality",
            Scenario = "walking-preview",
            FrameCount = 2,
            AverageTotalMs = 20
        };
        var interactive = new InteractivePerformanceRunSummary
        {
            Strategy = "InteractivePerformance",
            Scenario = "walking-preview",
            FrameCount = 2,
            AverageTotalMs = 10,
            InteractiveReturnRatio = 1
        };

        var snapshot = InteractiveRunComparisonSnapshotBuilder.Build("walking-preview", reference, interactive);

        Assert.Equal(2, snapshot.SpeedupRatio);
        Assert.Equal(1, snapshot.InteractiveReturnRatio);
    }

    [Fact]
    public void Acceptance_checklist_passes_healthy_comparison()
    {
        var snapshot = new InteractiveRunComparisonSnapshot(
            "walking-preview",
            ReferenceAverageMs: 20,
            InteractiveAverageMs: 10,
            SpeedupRatio: 2,
            FallbackRatio: 0,
            InteractiveReturnRatio: 1);
        var evidence = InteractiveEvidenceReporter.Build(
            "evidence",
            new InteractiveEvidenceBag().Add("ok", "true"));

        var checklist = InteractiveAcceptanceChecklistEvaluator.BuildDefault(snapshot, evidence);

        Assert.True(checklist.Passed);
        Assert.Equal(0, checklist.FailedCount);
    }

    [Fact]
    public void Acceptance_checklist_fails_slow_interactive_run()
    {
        var snapshot = new InteractiveRunComparisonSnapshot(
            "walking-preview",
            ReferenceAverageMs: 10,
            InteractiveAverageMs: 20,
            SpeedupRatio: 0.5,
            FallbackRatio: 0,
            InteractiveReturnRatio: 1);
        var evidence = InteractiveEvidenceReporter.Build(
            "evidence",
            new InteractiveEvidenceBag().Add("ok", "true"));

        var checklist = InteractiveAcceptanceChecklistEvaluator.BuildDefault(snapshot, evidence);

        Assert.False(checklist.Passed);
        Assert.True(checklist.FailedCount > 0);
    }

    [Fact]
    public void Default_scenario_matrix_has_preview_and_quality_rows()
    {
        var matrix = InteractiveEvidenceScenarioMatrixBuilder.BuildDefault();

        Assert.True(matrix.Count >= 4);
        Assert.Contains(matrix.Rows, row => row.QualityMode == "Preview");
        Assert.Contains(matrix.Rows, row => row.QualityMode == "Quality");
    }
}
