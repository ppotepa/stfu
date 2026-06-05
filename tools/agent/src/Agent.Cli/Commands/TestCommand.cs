using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static partial class TestCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("test", root, Path.GetRelativePath(root, solution));
        var target = options.Get("project") ?? config.DefaultSolution;
        var results = Path.Combine("artifacts", "test-results");
        var args = new List<string> { "test", target, "--logger", "trx", "--results-directory", results };
        if (options.Get("filter") is { Length: > 0 } filter) args.AddRange(["--filter", filter]);
        if (options.Get("configuration") is { Length: > 0 } configuration) args.AddRange(["--configuration", configuration]);
        if (options.Get("no-build") == "true") args.Add("--no-build");

        var result = await ProcessRunner.RunAsync("dotnet", args, root);
        foreach (var line in TextLines.Split(result.Stdout + "\n" + result.Stderr))
        {
            var match = FailedRegex().Match(line);
            if (!match.Success) continue;
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = "testFailure",
                ["testName"] = match.Groups["name"].Value.Trim(),
                ["outcome"] = "Failed",
                ["message"] = line.Trim()
            });
            if (envelope.Items.Count >= options.Max) break;
        }

        foreach (var failure in ReadTrxFailures(Path.Combine(root, results)).Take(Math.Max(0, options.Max - envelope.Items.Count)))
        {
            envelope.Items.Add(failure);
        }

        envelope.ToolExitCode = result.ExitCode == 0 ? 0 : 3;
        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }

    private static IEnumerable<Dictionary<string, object?>> ReadTrxFailures(string resultsDir)
    {
        if (!Directory.Exists(resultsDir)) yield break;
        foreach (var trx in Directory.EnumerateFiles(resultsDir, "*.trx", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).Take(3))
        {
            XDocument document;
            try { document = XDocument.Load(trx); }
            catch { continue; }
            foreach (var result in document.Descendants().Where(node => node.Name.LocalName == "UnitTestResult" && node.Attribute("outcome")?.Value == "Failed"))
            {
                yield return new Dictionary<string, object?>
                {
                    ["itemType"] = "testFailure",
                    ["testName"] = result.Attribute("testName")?.Value ?? result.Attribute("testId")?.Value,
                    ["outcome"] = "Failed",
                    ["duration"] = result.Attribute("duration")?.Value,
                    ["message"] = result.Descendants().FirstOrDefault(node => node.Name.LocalName == "Message")?.Value,
                    ["stack"] = result.Descendants().FirstOrDefault(node => node.Name.LocalName == "StackTrace")?.Value,
                    ["trx"] = trx
                };
            }
        }
    }

    [GeneratedRegex(@"Failed\s+(?<name>\S+)")]
    private static partial Regex FailedRegex();
}
