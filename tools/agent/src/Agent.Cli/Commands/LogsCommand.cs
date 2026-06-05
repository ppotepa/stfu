using System.Diagnostics;
using System.Text.Json;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class LogsCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("logs", root, Path.GetRelativePath(root, solution));
        var query = options.Get("query");
        var candidates = new[] { Path.Combine(root, "logs"), Path.Combine(root, ".logs", "agent"), Path.Combine(root, "artifacts") }
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories))
            .Where(path => Path.GetFileName(path).Equals("run.json", StringComparison.OrdinalIgnoreCase) || path.Contains("metrics", StringComparison.OrdinalIgnoreCase));

        foreach (var file in candidates.OrderByDescending(File.GetLastWriteTimeUtc))
        {
            var text = File.ReadAllText(file);
            if (!string.IsNullOrWhiteSpace(query) && !text.Contains(query, StringComparison.OrdinalIgnoreCase) && !file.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            envelope.Items.Add(ReadLogItem(root, file, text));
            if (envelope.Items.Count >= options.Max) break;
        }

        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = envelope.Items.Count >= options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }

    private static Dictionary<string, object?> ReadLogItem(string root, string file, string text)
    {
        var item = new Dictionary<string, object?>
        {
            ["itemType"] = "logRun",
            ["path"] = Path.GetRelativePath(root, file),
            ["startedAt"] = File.GetLastWriteTime(file).ToString("O")
        };
        try
        {
            using var document = JsonDocument.Parse(text);
            foreach (var property in document.RootElement.EnumerateObject().Take(12))
            {
                item[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt64(out var number) ? number : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => property.Value.ToString()
                };
            }
        }
        catch
        {
            item["message"] = text.Length > 200 ? text[..200] : text;
        }

        return item;
    }
}
