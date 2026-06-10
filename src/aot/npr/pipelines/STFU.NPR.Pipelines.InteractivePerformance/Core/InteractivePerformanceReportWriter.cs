using System.Globalization;
using System.Text;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractivePerformanceReportWriter
{
    public static string WriteSummary(InteractivePerformanceGateResult result, InteractiveStageBudgetPlan? budgetPlan = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine("STFU Interactive Performance gate");
        builder.AppendLine($"Scenario: {result.Scenario}");
        builder.AppendLine($"Status: {result.Status}");
        builder.AppendLine($"Reference average ms: {Format(result.ReferenceAverageMs)}");
        builder.AppendLine($"Interactive average ms: {Format(result.InteractiveAverageMs)}");
        builder.AppendLine($"Speedup ratio: {Format(result.SpeedupRatio)}");
        builder.AppendLine($"Interactive return ratio: {Format(result.InteractiveReturnRatio)}");
        builder.AppendLine($"Reference fallback ratio: {Format(result.ReferenceFallbackRatio)}");
        builder.AppendLine($"Self-contained projection ratio: {Format(result.SelfContainedProjectionRatio)}");
        builder.AppendLine($"Projected triangle candidate ratio: {Format(result.ProjectedTriangleCandidateRatio)}");
        builder.AppendLine($"Average health score: {Format(result.AverageHealthScore)}");

        if (budgetPlan is not null)
        {
            builder.AppendLine($"Target frame ms: {Format(budgetPlan.TargetFrameMs)}");
            builder.AppendLine($"Planned stage budget ms: {Format(budgetPlan.PlannedMs)}");
        }

        foreach (var warning in result.Warnings)
        {
            builder.AppendLine($"WARNING: {warning}");
        }

        foreach (var failure in result.Failures)
        {
            builder.AppendLine($"FAILURE: {failure}");
        }

        return builder.ToString();
    }

    public static string WriteCsv(IEnumerable<InteractivePerformanceRunSummary> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        var builder = new StringBuilder();
        builder.AppendLine("strategy,scenario,frames,avgTotalMs,p95TotalMs,interactiveReturnRatio,referenceFallbackRatio,selfContainedProjectionRatio,projectedTriangleCandidateRatio,avgHealthScore");
        foreach (var summary in summaries)
        {
            builder.Append(summary.Strategy).Append(',')
                .Append(summary.Scenario).Append(',')
                .Append(summary.FrameCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Format(summary.AverageTotalMs)).Append(',')
                .Append(Format(summary.P95TotalMs)).Append(',')
                .Append(Format(summary.InteractiveReturnRatio)).Append(',')
                .Append(Format(summary.ReferenceFallbackRatio)).Append(',')
                .Append(Format(summary.SelfContainedProjectionRatio)).Append(',')
                .Append(Format(summary.ProjectedTriangleCandidateRatio)).Append(',')
                .Append(Format(summary.AverageHealthScore))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
