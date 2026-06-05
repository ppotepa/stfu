using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class ConcatCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("concat", root, Path.GetRelativePath(root, solution));
        var locations = options.FromStdin ? StdinItems.ReadLocations().Take(options.Max).ToArray() : [];
        var budgetLines = int.TryParse(options.Get("budget-lines"), out var parsed) ? parsed : 300;
        var usedLines = 0;

        foreach (var location in locations)
        {
            var file = Path.Combine(root, location.File);
            if (!File.Exists(file)) continue;
            var lines = File.ReadAllLines(file);
            var start = Math.Max(1, location.Line - Math.Max(options.Context, 8));
            var end = Math.Min(lines.Length, location.Line + Math.Max(options.Context, 18));
            if (usedLines + (end - start + 1) > budgetLines) break;
            usedLines += end - start + 1;
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = "context",
                ["file"] = Path.GetRelativePath(root, file),
                ["line"] = location.Line,
                ["code"] = string.Join(Environment.NewLine, lines.Skip(start - 1).Take(end - start + 1))
            });
        }

        envelope.Summary.Count = locations.Length;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = envelope.Items.Count < locations.Length;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }
}
