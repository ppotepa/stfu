using System.Diagnostics;
using System.Text.RegularExpressions;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static partial class BuildCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("build", root, Path.GetRelativePath(root, solution));
        var target = options.Get("project") ?? options.Get("solution") ?? config.DefaultSolution;
        var args = new List<string> { "build", target, "-v", "minimal" };
        if (options.NoRestore) args.Add("--no-restore");
        if (options.Get("configuration") is { Length: > 0 } configuration) args.AddRange(["--configuration", configuration]);

        var result = await ProcessRunner.RunAsync("dotnet", args, root);
        var diagnostics = ParseDiagnostics(root, result.Stdout + Environment.NewLine + result.Stderr)
            .GroupBy(item => $"{item.GetValueOrDefault("file")}|{item.GetValueOrDefault("line")}|{item.GetValueOrDefault("column")}|{item.GetValueOrDefault("code")}|{item.GetValueOrDefault("message")}")
            .Select(group => group.First())
            .ToArray();
        foreach (var diagnostic in diagnostics.Take(options.Max))
        {
            envelope.Items.Add(diagnostic);
            envelope.Diagnostics.Add(diagnostic);
        }

        envelope.Summary.Count = diagnostics.Length;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = diagnostics.Length > options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        envelope.ToolExitCode = result.ExitCode == 0 ? 0 : 3;
        envelope.Ok = true;
        envelope.Next.Add(@".\tools\agent\agent.ps1 member --from-stdin --context 12 --format markdown");
        return envelope;
    }

    private static IEnumerable<Dictionary<string, object?>> ParseDiagnostics(string root, string output)
    {
        foreach (var line in TextLines.Split(output))
        {
            var match = DiagnosticRegex().Match(line);
            if (!match.Success) continue;
            var file = match.Groups["file"].Value.Trim();
            if (Path.IsPathRooted(file)) file = Path.GetRelativePath(root, file);
            yield return new Dictionary<string, object?>
            {
                ["itemType"] = "diagnostic",
                ["severity"] = match.Groups["severity"].Value,
                ["code"] = match.Groups["code"].Value,
                ["message"] = match.Groups["message"].Value,
                ["file"] = file,
                ["line"] = int.Parse(match.Groups["line"].Value),
                ["column"] = int.Parse(match.Groups["column"].Value),
                ["raw"] = line
            };
        }
    }

    [GeneratedRegex(@"^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\):\s+(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s+(?<message>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex DiagnosticRegex();
}
