using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveOutputHealthAnalyzerTests
{
    [Fact]
    public void Analyze_reports_no_artifacts_when_output_contract_has_no_readiness()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            WorkClass = InteractiveWorkClass.VisibilityRefresh,
            OutputReadiness = InteractiveOutputReadiness.None,
            OutputReadinessScore = 0,
            ReturnedReferenceFallback = true
        };

        var report = InteractiveOutputHealthAnalyzer.Analyze(diagnostics);

        Assert.Equal(InteractiveOutputHealthStatus.NoInteractiveArtifacts, report.Status);
        Assert.Equal(0, report.Score);
        Assert.True(report.WarningCount >= 1);
        Assert.Contains("NoInteractiveArtifacts", report.Summary);
    }

    [Fact]
    public void Analyze_reports_projection_only_when_only_projected_geometry_exists()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            OutputReadiness = InteractiveOutputReadiness.ProjectionReady,
            OutputReadinessScore = 10,
            OutputProjectedTriangles = 12
        };

        var report = InteractiveOutputHealthAnalyzer.Analyze(diagnostics);

        Assert.Equal(InteractiveOutputHealthStatus.ProjectionOnly, report.Status);
        Assert.Equal(15, report.Score);
    }

    [Fact]
    public void Analyze_reports_visible_geometry_for_candidate_edges()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            OutputReadiness = InteractiveOutputReadiness.CandidateEdgesReady,
            OutputReadinessScore = 40,
            OutputCandidateEdges = 24
        };

        var report = InteractiveOutputHealthAnalyzer.Analyze(diagnostics);

        Assert.Equal(InteractiveOutputHealthStatus.VisibleGeometry, report.Status);
        Assert.Equal(40, report.Score);
    }

    [Fact]
    public void Analyze_reports_stroke_data_ready_for_visible_segments()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            OutputReadiness = InteractiveOutputReadiness.VisibleSegmentsReady,
            OutputReadinessScore = 70,
            OutputVisibleStrokeSegments = 18
        };

        var report = InteractiveOutputHealthAnalyzer.Analyze(diagnostics);

        Assert.Equal(InteractiveOutputHealthStatus.StrokeDataReady, report.Status);
        Assert.Equal(70, report.Score);
    }

    [Fact]
    public void Analyze_reports_preview_candidate_when_frame_is_renderable_but_not_returned()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            OutputReadiness = InteractiveOutputReadiness.PreviewReady,
            OutputReadinessScore = 100,
            InteractivePreviewCandidateAvailable = true,
            ReturnedReferenceFallback = true
        };

        var report = InteractiveOutputHealthAnalyzer.Analyze(diagnostics);

        Assert.Equal(InteractiveOutputHealthStatus.ReturningReferenceFallback, report.Status);
        Assert.Equal(95, report.Score);
        Assert.True(report.WarningCount >= 1);
    }

    [Fact]
    public void Analyze_reports_returning_interactive_preview_when_opt_in_output_is_selected()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            OutputReadiness = InteractiveOutputReadiness.PreviewReady,
            OutputReadinessScore = 100,
            InteractivePreviewCandidateAvailable = true,
            ReturnedInteractiveFrame = true,
            ReturnedInteractiveFramePaths = 2,
            ReturnedInteractiveFrameSegments = 8
        };

        var report = InteractiveOutputHealthAnalyzer.Analyze(diagnostics);

        Assert.Equal(InteractiveOutputHealthStatus.ReturningInteractivePreview, report.Status);
        Assert.Equal(100, report.Score);
        Assert.Equal(0, report.WarningCount);
    }
}
