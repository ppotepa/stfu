namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractivePerformanceRunAggregator
{
    public static InteractivePerformanceRunSummary Summarize(string strategy, string scenario, IEnumerable<InteractivePerformanceRunSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var data = samples.Where(static sample => sample.TotalMs >= 0).ToArray();
        if (data.Length == 0)
        {
            return new InteractivePerformanceRunSummary
            {
                Strategy = strategy,
                Scenario = scenario,
                FrameCount = 0
            };
        }

        var totals = data.Select(static sample => sample.TotalMs).Order().ToArray();
        return new InteractivePerformanceRunSummary
        {
            Strategy = strategy,
            Scenario = scenario,
            FrameCount = data.Length,
            AverageTotalMs = data.Average(static sample => sample.TotalMs),
            P95TotalMs = Percentile(totals, 0.95),
            AverageProjectionMs = data.Average(static sample => sample.ProjectionMs),
            AverageVisibilityMs = data.Average(static sample => sample.VisibilityMs),
            AverageCandidateMs = data.Average(static sample => sample.CandidateMs),
            AverageStrokeMs = data.Average(static sample => sample.StrokeMs),
            AverageToneMs = data.Average(static sample => sample.ToneMs),
            InteractiveReturnRatio = Ratio(data.Count(static sample => sample.ReturnedInteractiveFrame), data.Length),
            ReferenceFallbackRatio = Ratio(data.Count(static sample => sample.ReturnedReferenceFallback), data.Length),
            SelfContainedProjectionRatio = Ratio(data.Count(static sample => sample.ProjectionBuiltSelfContained), data.Length),
            ProjectedTriangleCandidateRatio = Ratio(data.Count(static sample => sample.CandidateEdgesBuiltFromProjectedTriangles), data.Length),
            AverageHealthScore = data.Average(static sample => sample.HealthScore)
        };
    }

    private static double Ratio(int count, int total)
    {
        return total <= 0 ? 0 : (double)count / total;
    }

    private static double Percentile(IReadOnlyList<double> orderedValues, double percentile)
    {
        if (orderedValues.Count == 0)
        {
            return 0;
        }

        if (orderedValues.Count == 1)
        {
            return orderedValues[0];
        }

        var index = Math.Clamp(percentile, 0, 1) * (orderedValues.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
        {
            return orderedValues[lower];
        }

        return orderedValues[lower] + ((orderedValues[upper] - orderedValues[lower]) * (index - lower));
    }
}
