namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractivePerformanceGateEvaluator
{
    public static InteractivePerformanceGateResult Evaluate(
        InteractivePerformanceRunSummary reference,
        InteractivePerformanceRunSummary interactive,
        InteractivePerformanceGateThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(interactive);
        ArgumentNullException.ThrowIfNull(thresholds);

        var warnings = new List<string>();
        var failures = new List<string>();
        var speedupRatio = interactive.AverageTotalMs <= 0
            ? 0
            : reference.AverageTotalMs / interactive.AverageTotalMs;

        if (!reference.HasFrames)
        {
            failures.Add("Reference Quality summary has no frames.");
        }

        if (!interactive.HasFrames)
        {
            failures.Add("Interactive Performance summary has no frames.");
        }

        if (interactive.AverageTotalMs > thresholds.FailFrameMs)
        {
            failures.Add($"Interactive average frame time {interactive.AverageTotalMs:0.###} ms exceeds fail threshold {thresholds.FailFrameMs:0.###} ms.");
        }
        else if (interactive.AverageTotalMs > thresholds.WarningFrameMs)
        {
            warnings.Add($"Interactive average frame time {interactive.AverageTotalMs:0.###} ms exceeds warning threshold {thresholds.WarningFrameMs:0.###} ms.");
        }

        if (speedupRatio < thresholds.WarningSpeedupRatio)
        {
            failures.Add($"Interactive speedup ratio {speedupRatio:0.###} is below warning floor {thresholds.WarningSpeedupRatio:0.###}.");
        }
        else if (speedupRatio < thresholds.MinimumSpeedupRatio)
        {
            warnings.Add($"Interactive speedup ratio {speedupRatio:0.###} is below target {thresholds.MinimumSpeedupRatio:0.###}.");
        }

        if (interactive.ReferenceFallbackRatio > thresholds.MaximumReferenceFallbackRatio)
        {
            failures.Add($"Reference fallback ratio {interactive.ReferenceFallbackRatio:0.###} exceeds {thresholds.MaximumReferenceFallbackRatio:0.###}.");
        }

        if (interactive.InteractiveReturnRatio < thresholds.MinimumInteractiveReturnRatio)
        {
            failures.Add($"Interactive return ratio {interactive.InteractiveReturnRatio:0.###} is below {thresholds.MinimumInteractiveReturnRatio:0.###}.");
        }

        if (interactive.SelfContainedProjectionRatio < thresholds.MinimumSelfContainedProjectionRatio)
        {
            warnings.Add($"Self-contained projection ratio {interactive.SelfContainedProjectionRatio:0.###} is below {thresholds.MinimumSelfContainedProjectionRatio:0.###}.");
        }

        if (interactive.ProjectedTriangleCandidateRatio < thresholds.MinimumProjectedTriangleCandidateRatio)
        {
            warnings.Add($"Projected-triangle candidate ratio {interactive.ProjectedTriangleCandidateRatio:0.###} is below {thresholds.MinimumProjectedTriangleCandidateRatio:0.###}.");
        }

        if (interactive.AverageHealthScore < thresholds.MinimumHealthScore)
        {
            failures.Add($"Average health score {interactive.AverageHealthScore:0.###} is below {thresholds.MinimumHealthScore:0.###}.");
        }

        var status = failures.Count > 0
            ? InteractivePerformanceGateStatus.Fail
            : warnings.Count > 0
                ? InteractivePerformanceGateStatus.Warning
                : InteractivePerformanceGateStatus.Pass;

        return new InteractivePerformanceGateResult
        {
            Status = status,
            Scenario = interactive.Scenario,
            ReferenceAverageMs = reference.AverageTotalMs,
            InteractiveAverageMs = interactive.AverageTotalMs,
            SpeedupRatio = speedupRatio,
            InteractiveReturnRatio = interactive.InteractiveReturnRatio,
            ReferenceFallbackRatio = interactive.ReferenceFallbackRatio,
            SelfContainedProjectionRatio = interactive.SelfContainedProjectionRatio,
            ProjectedTriangleCandidateRatio = interactive.ProjectedTriangleCandidateRatio,
            AverageHealthScore = interactive.AverageHealthScore,
            Warnings = warnings,
            Failures = failures
        };
    }
}
