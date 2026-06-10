using System.Globalization;
using System.Text;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveFrameBenchmarkReporter
{
    public static InteractiveFrameBenchmarkReport BuildReport(IEnumerable<InteractiveFrameDiagnostics> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var samples = diagnostics
            .Where(item => item is not null)
            .Select(InteractiveFrameBenchmarkSample.FromDiagnostics)
            .ToArray();
        return samples.Length == 0
            ? InteractiveFrameBenchmarkReport.Empty
            : new InteractiveFrameBenchmarkReport(samples);
    }

    public static string WriteSummary(InteractiveFrameBenchmarkReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("STFU Interactive Performance benchmark report");
        builder.AppendLine(FormattableString.Invariant($"status: {report.EffectiveStatus}"));
        builder.AppendLine(FormattableString.Invariant($"samples: {report.SampleCount}"));
        builder.AppendLine(FormattableString.Invariant($"avg_stage_ms: {report.AverageStageMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"max_stage_ms: {report.MaxStageMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"avg_projection_ms: {report.AverageProjectionMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"avg_visibility_ms: {report.AverageVisibilityMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"avg_candidate_ms: {report.AverageCandidateMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"avg_stroke_plan_ms: {report.AverageStrokePlanMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"avg_tone_plan_ms: {report.AverageTonePlanMs:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"interactive_return_ratio: {report.InteractiveReturnRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"reference_fallback_ratio: {report.ReferenceFallbackRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"self_contained_projection_ratio: {report.SelfContainedProjectionRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"projected_triangle_candidate_ratio: {report.ProjectedTriangleCandidateRatio:0.###}"));
        builder.AppendLine(FormattableString.Invariant($"avg_health_score: {report.AverageHealthScore:0.###}"));
        return builder.ToString();
    }

    public static string WriteCsv(InteractiveFrameBenchmarkReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("frameId,qualityMode,workClass,totalStageMs,projectionMs,visibilityMs,candidateMs,strokePlanMs,tonePlanMs,projectedTriangles,visibleFaces,candidateEdges,strokeCommands,visibleStrokeSegments,toneRegions,returnedInteractiveFrame,returnedReferenceFallback,projectionBuiltSelfContained,candidateEdgesBuiltFromProjectedTriangles,outputHealthStatus,outputHealthScore,previewDecision,budgetPressure");
        foreach (var sample in report.Samples)
        {
            AppendCsv(builder, sample);
        }

        return builder.ToString();
    }

    private static void AppendCsv(StringBuilder builder, InteractiveFrameBenchmarkSample sample)
    {
        builder
            .Append(sample.FrameId).Append(',')
            .Append(sample.QualityMode).Append(',')
            .Append(sample.WorkClass).Append(',')
            .Append(Format(sample.TotalStageMs)).Append(',')
            .Append(Format(sample.ProjectionMs)).Append(',')
            .Append(Format(sample.VisibilityMs)).Append(',')
            .Append(Format(sample.CandidateMs)).Append(',')
            .Append(Format(sample.StrokePlanMs)).Append(',')
            .Append(Format(sample.TonePlanMs)).Append(',')
            .Append(sample.ProjectedTriangles).Append(',')
            .Append(sample.VisibleFaces).Append(',')
            .Append(sample.CandidateEdges).Append(',')
            .Append(sample.StrokeCommands).Append(',')
            .Append(sample.VisibleStrokeSegments).Append(',')
            .Append(sample.ToneRegions).Append(',')
            .Append(sample.ReturnedInteractiveFrame ? 1 : 0).Append(',')
            .Append(sample.ReturnedReferenceFallback ? 1 : 0).Append(',')
            .Append(sample.ProjectionBuiltSelfContained ? 1 : 0).Append(',')
            .Append(sample.CandidateEdgesBuiltFromProjectedTriangles ? 1 : 0).Append(',')
            .Append(sample.OutputHealthStatus).Append(',')
            .Append(sample.OutputHealthScore).Append(',')
            .Append(sample.PreviewDecision).Append(',')
            .Append(sample.BudgetPressure)
            .AppendLine();
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
