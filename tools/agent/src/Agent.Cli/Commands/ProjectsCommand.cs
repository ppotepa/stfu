using System.Diagnostics;
using System.Xml.Linq;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class ProjectsCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var projects = ProjectScanner.FindProjects(root, solution);
        var envelope = AgentEnvelope.Create("projects", root, Path.GetRelativePath(root, solution));
        if (options.Get("refresh") != "true" &&
            !options.ChangedOnly &&
            options.Get("affected") != "true" &&
            CacheStore.IsAvailable(root, config, "projects"))
        {
            envelope.Items.AddRange(CacheStore.ReadItems(root, config, "projects", options));
            envelope.Summary.Count = envelope.Items.Count;
            envelope.Summary.Returned = envelope.Items.Count;
            envelope.Summary.Cache = "warm";
            envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
            return envelope;
        }
        var changedFiles = options.ChangedOnly || options.Get("affected") == "true"
            ? ReadChangedFiles(root)
            : [];
        var projectItems = projects.Select(project => ReadProjectItem(root, project)).ToArray();
        var changedProjects = changedFiles.Count > 0
            ? projectItems.Where(item => ProjectContainsChangedFile(root, item, changedFiles)).Select(item => item["project"]?.ToString() ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var affectedProjects = options.Get("affected") == "true" ? ExpandAffected(projectItems, changedProjects) : changedProjects;

        foreach (var info in projectItems)
        {
            var projectName = info["project"]?.ToString() ?? "";
            info["changedFiles"] = CountChangedFiles(root, info, changedFiles);
            info["affectedByChangedFiles"] = affectedProjects.Contains(projectName);

            if (options.ChangedOnly && !affectedProjects.Contains(projectName))
            {
                continue;
            }

            if (options.Get("project") is { Length: > 0 } projectFilter &&
                !projectName.Contains(projectFilter, StringComparison.OrdinalIgnoreCase) &&
                !(info["path"]?.ToString()?.Contains(projectFilter, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                continue;
            }

            if (options.Get("package") is { Length: > 0 } packageFilter &&
                info.TryGetValue("packageReferences", out var packagesValue) &&
                packagesValue is Dictionary<string, object?>[] packages &&
                !packages.Any(package => package["name"]?.ToString()?.Contains(packageFilter, StringComparison.OrdinalIgnoreCase) == true))
            {
                continue;
            }

            envelope.Items.Add(info);
            if (envelope.Items.Count >= options.Max) break;
        }

        envelope.Summary.Count = projects.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = projects.Count > options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        envelope.Next.Add(@".\tools\agent\agent.ps1 build --format json");
        CacheStore.WriteJsonLines(root, config, "projects.jsonl", envelope.Items);
        return envelope;
    }

    private static IReadOnlyList<string> ReadChangedFiles(string root)
    {
        var status = ProcessRunner.RunAsync("git", ["status", "--porcelain"], root).GetAwaiter().GetResult().Stdout;
        return TextLines.Split(status)
            .Select(line => line.Length > 3 ? line[3..] : line)
            .ToArray();
    }

    private static bool ProjectContainsChangedFile(string root, Dictionary<string, object?> project, IReadOnlyList<string> changedFiles)
    {
        return CountChangedFiles(root, project, changedFiles) > 0;
    }

    private static int CountChangedFiles(string root, Dictionary<string, object?> project, IReadOnlyList<string> changedFiles)
    {
        var projectPath = project["path"]?.ToString();
        if (string.IsNullOrWhiteSpace(projectPath)) return 0;
        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        return changedFiles.Count(file => file.Replace('\\', '/').StartsWith(projectDir.Replace('\\', '/').TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> ExpandAffected(Dictionary<string, object?>[] projectItems, HashSet<string> changedProjects)
    {
        var affected = new HashSet<string>(changedProjects, StringComparer.OrdinalIgnoreCase);
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var project in projectItems)
            {
                var name = project["project"]?.ToString() ?? "";
                if (affected.Contains(name)) continue;
                if (project.TryGetValue("projectReferences", out var refsValue) &&
                    refsValue is string[] refs &&
                    refs.Any(affected.Contains))
                {
                    affected.Add(name);
                    changed = true;
                }
            }
        }

        return affected;
    }

    public static Dictionary<string, object?> ReadProjectItem(string root, string path)
    {
        var document = XDocument.Load(path);
        var tfms = document.Descendants().Where(node => node.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .Select(node => node.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct()
            .ToArray();
        var projectReferences = document.Descendants().Where(node => node.Name.LocalName == "ProjectReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value))
            .ToArray();
        var packageReferences = document.Descendants().Where(node => node.Name.LocalName == "PackageReference")
            .Select(node => new Dictionary<string, object?>
            {
                ["name"] = node.Attribute("Include")?.Value ?? node.Attribute("Update")?.Value ?? "",
                ["version"] = node.Attribute("Version")?.Value ?? node.Elements().FirstOrDefault(child => child.Name.LocalName == "Version")?.Value ?? ""
            })
            .Where(item => !string.IsNullOrWhiteSpace(item["name"]?.ToString()))
            .ToArray();

        var name = Path.GetFileNameWithoutExtension(path);
        return new Dictionary<string, object?>
        {
            ["itemType"] = "project",
            ["project"] = name,
            ["path"] = Path.GetRelativePath(root, path),
            ["targetFrameworks"] = tfms,
            ["isTestProject"] = name.Contains("Test", StringComparison.OrdinalIgnoreCase),
            ["projectReferences"] = projectReferences,
            ["packageReferences"] = packageReferences
        };
    }
}
