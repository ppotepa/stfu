using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class SearchCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("search", root, Path.GetRelativePath(root, solution));
        var pattern = options.Get("pattern") ?? options.Get("arg0");
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return AgentEnvelope.Failure("search", root, Path.GetRelativePath(root, solution), "missing_pattern", "Provide --pattern <text>.");
        }

        var args = new List<string> { "--line-number", "--column", "--glob", "!**/bin/**", "--glob", "!**/obj/**" };
        if (options.Literal) args.Add("--fixed-strings");
        var type = options.Get("type");
        if (type == "cs") args.AddRange(["--glob", "*.cs"]);
        if (type == "json") args.AddRange(["--glob", "*.json"]);
        if (type == "ps1") args.AddRange(["--glob", "*.ps1"]);
        args.Add(pattern);
        args.Add(options.Get("path") ?? ".");

        var result = await ProcessRunner.RunAsync("rg", args, root);
        if (result.ExitCode > 1)
        {
            return AgentEnvelope.Failure("search", root, Path.GetRelativePath(root, solution), "search_failed", result.Stderr.Trim());
        }

        var lines = TextLines.Split(result.Stdout);
        foreach (var line in lines.Take(options.Max))
        {
            var parts = line.Split(':', 4);
            if (parts.Length < 4) continue;
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = "searchMatch",
                ["file"] = parts[0],
                ["line"] = int.TryParse(parts[1], out var lineNo) ? lineNo : 0,
                ["column"] = int.TryParse(parts[2], out var column) ? column : 0,
                ["text"] = parts[3].Trim(),
                ["language"] = Path.GetExtension(parts[0]).Equals(".cs", StringComparison.OrdinalIgnoreCase) ? "csharp" : "text",
                ["next"] = new[] { $@".\tools\agent\agent.ps1 member --file ""{parts[0]}"" --line {parts[1]}" }
            });
        }

        envelope.Summary.Count = lines.Length;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = lines.Length > options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }
}
