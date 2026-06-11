using System.Globalization;
using System.Text;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveEvidenceReporter
{
    public static InteractiveEvidenceReport Build(string title, InteractiveEvidenceBag bag)
    {
        ArgumentNullException.ThrowIfNull(bag);
        return new InteractiveEvidenceReport(title, bag.Facts);
    }

    public static string WriteSummary(InteractiveEvidenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine(report.Title);
        builder.AppendLine(FormattableString.Invariant($"status: {report.Status}"));
        builder.AppendLine(FormattableString.Invariant($"facts: {report.Facts.Count}"));
        builder.AppendLine(FormattableString.Invariant($"info: {report.InfoCount}"));
        builder.AppendLine(FormattableString.Invariant($"warnings: {report.WarningCount}"));
        builder.AppendLine(FormattableString.Invariant($"failures: {report.FailureCount}"));
        builder.AppendLine(FormattableString.Invariant($"hash: {report.StableHash}"));

        foreach (var fact in report.Facts)
        {
            builder.AppendLine(FormattableString.Invariant($"- [{fact.Severity}] {fact.Kind}:{fact.Name}={fact.Value}"));
        }

        return builder.ToString();
    }

    public static string WriteCsv(InteractiveEvidenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("kind,severity,name,value");
        foreach (var fact in report.Facts)
        {
            builder
                .Append(fact.Kind).Append(',')
                .Append(fact.Severity).Append(',')
                .Append(Escape(fact.Name)).Append(',')
                .Append(Escape(fact.Value)).AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
