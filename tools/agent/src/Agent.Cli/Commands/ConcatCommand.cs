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
        var filtered = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var location in locations)
        {
            var file = Path.GetFullPath(Path.Combine(root, location.File));
            if (!File.Exists(file)) continue;
            if (!PathFilter.IsAllowedForConcat(root, config, options, file, out var reason))
            {
                filtered[reason] = filtered.TryGetValue(reason, out var count) ? count + 1 : 1;
                continue;
            }

            var relativePath = PathFilter.Normalize(Path.GetRelativePath(root, file));
            if (!seen.Add($"{relativePath}:{location.Line}"))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            if (location.Line > lines.Length)
            {
                continue;
            }

            var start = Math.Max(1, location.Line - Math.Max(options.Context, 8));
            var end = Math.Min(lines.Length, location.Line + Math.Max(options.Context, 18));
            if (usedLines + (end - start + 1) > budgetLines) break;
            usedLines += end - start + 1;
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = "context",
                ["file"] = relativePath,
                ["line"] = location.Line,
                ["code"] = string.Join(Environment.NewLine, lines.Skip(start - 1).Take(end - start + 1))
            });
        }

        envelope.Summary.Count = locations.Length;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = envelope.Items.Count < locations.Length;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        foreach (var pair in filtered.OrderBy(pair => pair.Key))
        {
            envelope.Warnings.Add($"concat skipped {pair.Value} location(s): {pair.Key}");
        }

        if (filtered.Keys.Any(key => key.StartsWith("not-in-profile", StringComparison.OrdinalIgnoreCase) ||
                                     key.StartsWith("not-in-include", StringComparison.OrdinalIgnoreCase)))
        {
            envelope.Next.Add(@".\tools\agent\agent.ps1 concat --from-stdin --include <configured-scope> --format markdown");
            envelope.Next.Add(@".\tools\agent\agent.ps1 concat --from-stdin --profile all --format markdown");
        }

        return envelope;
    }
}
