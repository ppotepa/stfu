using System.Globalization;
using System.Text;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveBenchmarkComparisonReporter
{
    public static InteractiveBenchmarkComparisonReport BuildComparison(
        IEnumerable<InteractiveFrameDiagnostics> referenceDiagnostics,
        IEnumerable<InteractiveFrameDiagnostics> interactiveDiagnostics)
    {
        ArgumentNullException.ThrowIfNull(referenceDiagnostics);
        ArgumentNullException.ThrowIfNull(interactiveDiagnostics);

        return BuildComparison(
            InteractiveFrameBenchmarkReporter.BuildReport(referenceDiagnostics),
            InteractiveFrameBenchmarkReporter.BuildReport(interactiveDiagnostics));
    }

    public static InteractiveBenchmarkComparisonReport BuildComparison(
        InteractiveFrameBenchmarkReport reference,
        InteractiveFrameBenchmarkReport interactive)
    {
        return new InteractiveBenchmarkComparisonReport(reference, interactive);
    }

    public static string WriteSummary(InteractiveBenchmarkComparisonReport comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        var builder = new StringBuilder();
        builder.AppendLine("STFU Interactive Performance parity comparison");
        builder.AppendLine(FormattableString.Invariant($"status: {comparison.Status}"));
        builder.AppendLine(FormattableString.Invariant($"reference_samples: {comparison.Reference.SampleCount}"));
        builder.AppendLine(FormattableString.Invariant($"interactive_samples: {comparison.Interactive.SampleCount}"));
        builder.AppendLine(FormattableString.Invariant($"reference_avg_stage_ms: {comparison.ReferenceAverageStageMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"interactive_avg_stage_ms: {comparison.InteractiveAverageStageMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"stage_delta_ms: {comparison.StageDeltaMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"speedup_ratio: {comparison.SpeedupRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"interactive_stage_ratio: {comparison.InteractiveStageRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"interactive_return_ratio: {comparison.InteractiveReturnRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"reference_fallback_ratio: {comparison.ReferenceFallbackRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"self_contained_projection_ratio: {comparison.SelfContainedProjectionRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"projected_triangle_candidate_ratio: {comparison.ProjectedTriangleCandidateRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"health_score_delta: {comparison.HealthScoreDelta:0.###}"));
        return builder.ToString();
    }

    public static string WriteCsv(InteractiveBenchmarkComparisonReport comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        var builder = new StringBuilder();
        builder.AppendLine("status,referenceSamples,interactiveSamples,referenceAvgStageMs,interactiveAvgStageMs,stageDeltaMs,speedupRatio,interactiveStageRatio,interactiveReturnRatio,referenceFallbackRatio,selfContainedProjectionRatio,projectedTriangleCandidateRatio,healthScoreDelta");
        builder
            .Append(comparison.Status).Append(',')
            .Append(comparison.Reference.SampleCount).Append(',')
            .Append(comparison.Interactive.SampleCount).Append(',')
            .Append(Format(comparison.ReferenceAverageStageMs)).Append(',')
            .Append(Format(comparison.InteractiveAverageStageMs)).Append(',')
            .Append(Format(comparison.StageDeltaMs)).Append(',')
            .Append(Format(comparison.SpeedupRatio)).Append(',')
            .Append(Format(comparison.InteractiveStageRatio)).Append(',')
            .Append(Format(comparison.InteractiveReturnRatio)).Append(',')
            .Append(Format(comparison.ReferenceFallbackRatio)).Append(',')
            .Append(Format(comparison.SelfContainedProjectionRatio)).Append(',')
            .Append(Format(comparison.ProjectedTriangleCandidateRatio)).Append(',')
            .Append(Format(comparison.HealthScoreDelta))
            .AppendLine();
        return builder.ToString();
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
