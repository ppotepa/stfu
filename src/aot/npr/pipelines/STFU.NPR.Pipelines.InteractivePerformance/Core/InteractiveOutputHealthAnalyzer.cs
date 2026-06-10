using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveOutputHealthAnalyzer
{
    public static InteractiveOutputHealthReport Analyze(InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var status = ResolveStatus(diagnostics);
        var score = ResolveScore(diagnostics, status);
        var warningCount = CountWarnings(diagnostics, status);
        var summary = BuildSummary(diagnostics, status, score, warningCount);

        return new InteractiveOutputHealthReport(status, score, warningCount, summary);
    }

    private static InteractiveOutputHealthStatus ResolveStatus(InteractiveFrameDiagnostics diagnostics)
    {
        if (diagnostics.ReturnedInteractiveFrame)
        {
            return InteractiveOutputHealthStatus.ReturningInteractivePreview;
        }

        if (diagnostics.ReturnedReferenceFallback)
        {
            return diagnostics.InteractivePreviewCandidateAvailable
                ? InteractiveOutputHealthStatus.ReturningReferenceFallback
                : ResolveArtifactStatus(diagnostics);
        }

        return ResolveArtifactStatus(diagnostics);
    }

    private static InteractiveOutputHealthStatus ResolveArtifactStatus(InteractiveFrameDiagnostics diagnostics)
    {
        return diagnostics.OutputReadiness switch
        {
            InteractiveOutputReadiness.PreviewReady => InteractiveOutputHealthStatus.PreviewCandidateReady,
            InteractiveOutputReadiness.StrokeFrameReady => InteractiveOutputHealthStatus.PreviewCandidateReady,
            InteractiveOutputReadiness.VisibleSegmentsReady => InteractiveOutputHealthStatus.StrokeDataReady,
            InteractiveOutputReadiness.StrokeCommandsReady => InteractiveOutputHealthStatus.StrokeDataReady,
            InteractiveOutputReadiness.CandidateEdgesReady => InteractiveOutputHealthStatus.VisibleGeometry,
            InteractiveOutputReadiness.VisibilityReady => InteractiveOutputHealthStatus.VisibleGeometry,
            InteractiveOutputReadiness.ProjectionReady => InteractiveOutputHealthStatus.ProjectionOnly,
            _ => InteractiveOutputHealthStatus.NoInteractiveArtifacts
        };
    }

    private static int ResolveScore(
        InteractiveFrameDiagnostics diagnostics,
        InteractiveOutputHealthStatus status)
    {
        var score = Math.Clamp(diagnostics.OutputReadinessScore, 0, 100);
        var floor = status switch
        {
            InteractiveOutputHealthStatus.ReturningInteractivePreview => 100,
            InteractiveOutputHealthStatus.ReturningReferenceFallback => 85,
            InteractiveOutputHealthStatus.PreviewCandidateReady => 85,
            InteractiveOutputHealthStatus.StrokeDataReady => 60,
            InteractiveOutputHealthStatus.VisibleGeometry => 35,
            InteractiveOutputHealthStatus.ProjectionOnly => 15,
            _ => 0
        };

        score = Math.Max(score, floor);

        if (diagnostics.ReturnedReferenceFallback && diagnostics.InteractivePreviewCandidateAvailable)
        {
            score = Math.Min(score, 95);
        }

        if (diagnostics.ReturnedReferenceFallback && !diagnostics.InteractivePreviewCandidateAvailable)
        {
            score = Math.Min(score, 80);
        }

        return Math.Clamp(score, 0, 100);
    }

    private static int CountWarnings(
        InteractiveFrameDiagnostics diagnostics,
        InteractiveOutputHealthStatus status)
    {
        var warnings = 0;

        if (status == InteractiveOutputHealthStatus.NoInteractiveArtifacts)
        {
            warnings++;
        }

        if (diagnostics.ReturnedReferenceFallback && diagnostics.InteractivePreviewCandidateAvailable)
        {
            warnings++;
        }

        if (diagnostics.OutputReadinessScore < 55 &&
            diagnostics.WorkClass != InteractiveWorkClass.ReferenceFallback)
        {
            warnings++;
        }

        if (diagnostics.CacheMisses > diagnostics.CacheHits && diagnostics.FrameId > 1)
        {
            warnings++;
        }

        return warnings;
    }

    private static string BuildSummary(
        InteractiveFrameDiagnostics diagnostics,
        InteractiveOutputHealthStatus status,
        int score,
        int warningCount)
    {
        var output = diagnostics.ReturnedInteractiveFrame
            ? "interactive"
            : diagnostics.ReturnedReferenceFallback
                ? "reference"
                : "pending";

        return $"{status}: score {score}, warnings {warningCount}, output {output}, readiness {diagnostics.OutputReadiness}";
    }
}
