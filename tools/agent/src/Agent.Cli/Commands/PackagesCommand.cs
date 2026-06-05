using System.Diagnostics;
using System.Text.Json;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class PackagesCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("packages", root, Path.GetRelativePath(root, solution));
        var packageFilter = options.Get("package");

        foreach (var projectPath in ProjectScanner.FindProjects(root, solution))
        {
            var project = ProjectsCommand.ReadProjectItem(root, projectPath);
            if (project.TryGetValue("packageReferences", out var value) && value is Dictionary<string, object?>[] packages)
            {
                foreach (var package in packages)
                {
                    var name = package["name"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(packageFilter) && !name.Contains(packageFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    envelope.Items.Add(new Dictionary<string, object?>
                    {
                        ["itemType"] = "package",
                        ["name"] = name,
                        ["version"] = package["version"],
                        ["kind"] = "direct",
                        ["project"] = project["project"],
                        ["next"] = new[] { $@".\tools\agent\agent.ps1 projects --package {name}" }
                    });
                    if (envelope.Items.Count >= options.Max) break;
                }
            }
            if (options.Get("direct") != "true")
            {
                foreach (var package in ReadTransitivePackages(projectPath))
                {
                    var name = package.Name;
                    if (!string.IsNullOrWhiteSpace(packageFilter) && !name.Contains(packageFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    envelope.Items.Add(new Dictionary<string, object?>
                    {
                        ["itemType"] = "package",
                        ["name"] = name,
                        ["version"] = package.Version,
                        ["kind"] = "transitive",
                        ["project"] = project["project"]
                    });
                    if (envelope.Items.Count >= options.Max) break;
                }
            }
            if (envelope.Items.Count >= options.Max) break;
        }

        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = envelope.Items.Count >= options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }

    private static IEnumerable<(string Name, string Version)> ReadTransitivePackages(string projectPath)
    {
        var assets = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        if (!File.Exists(assets)) yield break;
        using var document = JsonDocument.Parse(File.ReadAllText(assets));
        if (!document.RootElement.TryGetProperty("libraries", out var libraries)) yield break;
        foreach (var library in libraries.EnumerateObject())
        {
            if (!library.Value.TryGetProperty("type", out var type) ||
                !type.GetString()!.Equals("package", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = library.Name.Split('/', 2);
            if (parts.Length == 2) yield return (parts[0], parts[1]);
        }
    }
}
