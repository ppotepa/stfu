namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveStageBudgetPlanner
{
    public static InteractiveStageBudgetPlan Plan(string scenario, double targetFrameMs)
    {
        if (string.IsNullOrWhiteSpace(scenario))
        {
            scenario = "interactive";
        }

        targetFrameMs = Math.Max(targetFrameMs, 1.0);
        return new InteractiveStageBudgetPlan
        {
            Scenario = scenario,
            TargetFrameMs = targetFrameMs,
            ProjectionBudgetMs = targetFrameMs * 0.16,
            VisibilityBudgetMs = targetFrameMs * 0.16,
            CandidateBudgetMs = targetFrameMs * 0.14,
            StrokeBudgetMs = targetFrameMs * 0.20,
            ToneBudgetMs = targetFrameMs * 0.10,
            GpuBudgetMs = targetFrameMs * 0.14,
            ReserveBudgetMs = targetFrameMs * 0.10
        };
    }

    public static IReadOnlyList<string> Compare(InteractiveStageBudgetPlan plan, InteractivePerformanceRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(summary);

        var warnings = new List<string>();
        if (summary.AverageProjectionMs > plan.ProjectionBudgetMs)
        {
            warnings.Add($"Projection is over budget: {summary.AverageProjectionMs:0.###} ms > {plan.ProjectionBudgetMs:0.###} ms.");
        }

        if (summary.AverageVisibilityMs > plan.VisibilityBudgetMs)
        {
            warnings.Add($"Visibility is over budget: {summary.AverageVisibilityMs:0.###} ms > {plan.VisibilityBudgetMs:0.###} ms.");
        }

        if (summary.AverageCandidateMs > plan.CandidateBudgetMs)
        {
            warnings.Add($"Candidate edge planning is over budget: {summary.AverageCandidateMs:0.###} ms > {plan.CandidateBudgetMs:0.###} ms.");
        }

        if (summary.AverageStrokeMs > plan.StrokeBudgetMs)
        {
            warnings.Add($"Stroke planning is over budget: {summary.AverageStrokeMs:0.###} ms > {plan.StrokeBudgetMs:0.###} ms.");
        }

        if (summary.AverageToneMs > plan.ToneBudgetMs)
        {
            warnings.Add($"Tone planning is over budget: {summary.AverageToneMs:0.###} ms > {plan.ToneBudgetMs:0.###} ms.");
        }

        return warnings;
    }
}
