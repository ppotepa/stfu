using STFU.NPR.Pipeline.InteractivePerformance.Core;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveEvidenceReplayBundleTests
{
    [Fact]
    public void Evidence_report_tracks_warnings_failures_and_hash()
    {
        var bag = new InteractiveEvidenceBag()
            .Add("metric.totalMs", "12", InteractiveEvidenceKind.Metric)
            .Add("gate.speedup", "0.8", InteractiveEvidenceKind.Gate, InteractiveEvidenceSeverity.Warning)
            .Add("regression.stage", "candidate", InteractiveEvidenceKind.Regression, InteractiveEvidenceSeverity.Failure);

        var report = InteractiveEvidenceReporter.Build("test evidence", bag);

        Assert.Equal(3, report.Facts.Count);
        Assert.Equal(1, report.WarningCount);
        Assert.Equal(1, report.FailureCount);
        Assert.Equal(InteractiveEvidenceSeverity.Failure, report.Status);
        Assert.False(string.IsNullOrWhiteSpace(report.StableHash));
    }

    [Fact]
    public void Evidence_report_writes_summary_and_csv()
    {
        var bag = new InteractiveEvidenceBag()
            .Add("metric.totalMs", "12", InteractiveEvidenceKind.Metric);

        var report = InteractiveEvidenceReporter.Build("test evidence", bag);
        var summary = InteractiveEvidenceReporter.WriteSummary(report);
        var csv = InteractiveEvidenceReporter.WriteCsv(report);

        Assert.Contains("test evidence", summary);
        Assert.Contains("metric.totalMs", csv);
    }

    [Fact]
    public void Archive_writer_builds_manifest_with_hashes()
    {
        var index = InteractiveEvidenceArchiveWriter.BuildIndex(new[]
        {
            ("summary.txt", "summary", "hello"),
            ("metrics.csv", "csv", "a,b")
        });

        var manifest = InteractiveEvidenceArchiveWriter.WriteManifest(index);

        Assert.Equal(2, index.Count);
        Assert.Contains("summary.txt", manifest);
        Assert.Contains("metrics.csv", manifest);
    }
}
