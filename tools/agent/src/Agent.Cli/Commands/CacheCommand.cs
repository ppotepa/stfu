using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class CacheCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var action = options.Get("arg0") ?? "status";
        var envelope = AgentEnvelope.Create("cache", root, Path.GetRelativePath(root, solution));
        CacheStore.Ensure(root, config);
        if (action.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            var dir = CacheStore.DirectoryPath(root, config);
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                try { File.Delete(file); } catch { }
            }
            CacheStore.Ensure(root, config);
        }

        foreach (var file in Directory.EnumerateFiles(CacheStore.DirectoryPath(root, config)).OrderBy(Path.GetFileName).Take(options.Max))
        {
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = "cacheFile",
                ["name"] = Path.GetFileName(file),
                ["path"] = Path.GetRelativePath(root, file),
                ["bytes"] = new FileInfo(file).Length,
                ["updatedAt"] = File.GetLastWriteTime(file).ToString("O")
            });
        }
        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }
}
