using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static partial class BenchHistoryCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("bench-history", root, Path.GetRelativePath(root, solution));
        var metric = options.Get("metric") ?? "frameMs";
        var metrics = ReadMetrics(root, metric).OrderByDescending(metricItem => metricItem.StartedAt).ToArray();

        for (var i = 0; i < metrics.Length && envelope.Items.Count < options.Max; i++)
        {
            var current = metrics[i];
            var previous = metrics.Skip(i + 1).FirstOrDefault(candidate => candidate.Metric == current.Metric && candidate.Benchmark == current.Benchmark);
            var delta = previous is null || previous.Value == 0 ? null : (double?)((current.Value - previous.Value) / previous.Value * 100.0);
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = delta is null ? "benchmarkMetric" : "benchmarkRegression",
                ["benchmark"] = current.Benchmark,
                ["metric"] = current.Metric,
                ["value"] = current.Value,
                ["unit"] = current.Unit,
                ["runId"] = current.RunId,
                ["baselineValue"] = previous?.Value,
                ["deltaPercent"] = delta,
                ["status"] = delta is > 5 ? "regressed" : delta is < -5 ? "improved" : "stable",
                ["path"] = current.Path,
                ["next"] = new[] { @".\tools\agent\agent.ps1 diff --semantic --format json", @".\tools\agent\agent.ps1 hotspots --changed-only --rank hotness --format json" }
            });
        }

        envelope.Summary.Count = metrics.Length;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = metrics.Length > envelope.Items.Count;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        envelope.Next.Add(@".\tools\agent\agent.ps1 diff --semantic --format json");
        return envelope;
    }

    private static IEnumerable<MetricRecord> ReadMetrics(string root, string preferredMetric)
    {
        var dirs = new[] { Path.Combine(root, "logs"), Path.Combine(root, ".logs", "agent"), Path.Combine(root, "artifacts") }.Where(Directory.Exists);
        foreach (var file in dirs.SelectMany(dir => Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)).OrderByDescending(File.GetLastWriteTimeUtc))
        {
            var text = File.ReadAllText(file);
            foreach (var metric in ReadJsonMetrics(root, file, text, preferredMetric))
                yield return metric;
            foreach (var metric in ReadRegexMetrics(root, file, text, preferredMetric))
                yield return metric;
        }
    }

    private static IEnumerable<MetricRecord> ReadJsonMetrics(string root, string file, string text, string preferredMetric)
    {
        JsonDocument document;
        try { document = JsonDocument.Parse(text); }
        catch { yield break; }
        var runId = document.RootElement.TryGetProperty("runId", out var run) ? run.GetString() : Path.GetFileName(Path.GetDirectoryName(file));
        var benchmark = document.RootElement.TryGetProperty("benchmark", out var bench) ? bench.GetString() : document.RootElement.TryGetProperty("commandLine", out var command) ? command.GetString() : Path.GetFileName(file);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!IsMetricName(property.Name, preferredMetric) || property.Value.ValueKind != JsonValueKind.Number) continue;
            yield return new MetricRecord(
                benchmark ?? "benchmark",
                property.Name,
                property.Value.GetDouble(),
                Unit(property.Name),
                runId ?? "",
                File.GetLastWriteTimeUtc(file),
                Path.GetRelativePath(root, file));
        }
    }

    private static IEnumerable<MetricRecord> ReadRegexMetrics(string root, string file, string text, string preferredMetric)
    {
        foreach (Match match in MetricRegex().Matches(text))
        {
            var name = match.Groups["name"].Value;
            if (!IsMetricName(name, preferredMetric)) continue;
            yield return new MetricRecord(Path.GetFileName(file), name, double.Parse(match.Groups["value"].Value), Unit(name), Path.GetFileName(Path.GetDirectoryName(file)) ?? "", File.GetLastWriteTimeUtc(file), Path.GetRelativePath(root, file));
        }
    }

    private static bool IsMetricName(string name, string preferredMetric)
    {
        return name.Equals(preferredMetric, StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith("Ms", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith("Milliseconds", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith("Bytes", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("alloc", StringComparison.OrdinalIgnoreCase);
    }

    private static string Unit(string name)
    {
        if (name.Contains("alloc", StringComparison.OrdinalIgnoreCase) || name.Contains("byte", StringComparison.OrdinalIgnoreCase)) return "bytes";
        if (name.Contains("ms", StringComparison.OrdinalIgnoreCase) || name.Contains("frame", StringComparison.OrdinalIgnoreCase)) return "ms";
        return "";
    }

    private sealed record MetricRecord(string Benchmark, string Metric, double Value, string Unit, string RunId, DateTime StartedAt, string Path);

    [GeneratedRegex(@"(?<name>[A-Za-z][A-Za-z0-9_.-]*(?:Ms|Bytes|frameMs|allocBytes))[""'\s:=]+(?<value>\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex MetricRegex();
}
