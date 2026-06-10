namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveRegressionAnalyzer
{
    public static InteractiveRegressionReport Analyze(
        InteractivePerformanceRunSummary baseline,
        InteractivePerformanceRunSummary current,
        InteractiveRegressionThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(thresholds);

        var findings = new List<InteractiveRegressionFinding>();
        AddIncreaseFinding(
            findings,
            "averageTotalMs",
            baseline.AverageTotalMs,
            current.AverageTotalMs,
            thresholds.WarningFrameMsDelta,
            thresholds.ErrorFrameMsDelta,
            "Average frame time regression");
        AddIncreaseFinding(
            findings,
            "referenceFallbackRatio",
            baseline.ReferenceFallbackRatio,
            current.ReferenceFallbackRatio,
            thresholds.WarningFallbackRatioDelta,
            thresholds.ErrorFallbackRatioDelta,
            "Reference fallback ratio regression");
        AddDropFinding(
            findings,
            "averageHealthScore",
            baseline.AverageHealthScore,
            current.AverageHealthScore,
            thresholds.WarningHealthScoreDrop,
            thresholds.ErrorHealthScoreDrop,
            "Health score regression");

        return new InteractiveRegressionReport
        {
            Scenario = current.Scenario,
            Findings = findings
        };
    }

    private static void AddIncreaseFinding(
        List<InteractiveRegressionFinding> findings,
        string metric,
        double baseline,
        double current,
        double warningDelta,
        double errorDelta,
        string message)
    {
        var delta = current - baseline;
        if (delta >= errorDelta)
        {
            findings.Add(Create(metric, InteractiveRegressionSeverity.Error, baseline, current, delta, message));
        }
        else if (delta >= warningDelta)
        {
            findings.Add(Create(metric, InteractiveRegressionSeverity.Warning, baseline, current, delta, message));
        }
    }

    private static void AddDropFinding(
        List<InteractiveRegressionFinding> findings,
        string metric,
        double baseline,
        double current,
        double warningDrop,
        double errorDrop,
        string message)
    {
        var delta = baseline - current;
        if (delta >= errorDrop)
        {
            findings.Add(Create(metric, InteractiveRegressionSeverity.Error, baseline, current, -delta, message));
        }
        else if (delta >= warningDrop)
        {
            findings.Add(Create(metric, InteractiveRegressionSeverity.Warning, baseline, current, -delta, message));
        }
    }

    private static InteractiveRegressionFinding Create(
        string metric,
        InteractiveRegressionSeverity severity,
        double baseline,
        double current,
        double delta,
        string message)
    {
        return new InteractiveRegressionFinding
        {
            Metric = metric,
            Severity = severity,
            Baseline = baseline,
            Current = current,
            Delta = delta,
            Message = message
        };
    }
}
