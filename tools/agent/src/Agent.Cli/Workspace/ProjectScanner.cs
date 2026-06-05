using System.Xml.Linq;

namespace Agent.Cli.Workspace;

public static class ProjectScanner
{
    public static string ResolveSolution(string root, AgentConfig config, ToolOptions options)
    {
        var explicitSolution = options.Get("solution");
        if (!string.IsNullOrWhiteSpace(explicitSolution))
        {
            return Path.GetFullPath(Path.Combine(root, explicitSolution));
        }

        var configured = Path.Combine(root, config.DefaultSolution);
        if (File.Exists(configured))
        {
            return configured;
        }

        return Directory.EnumerateFiles(root, "*.sln*", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? configured;
    }

    public static IReadOnlyList<string> FindProjects(string root, string solutionPath)
    {
        if (File.Exists(solutionPath) && Path.GetExtension(solutionPath).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var document = XDocument.Load(solutionPath);
                var paths = document.Descendants()
                    .Where(node => node.Name.LocalName == "Project")
                    .Select(node => node.Attribute("Path")?.Value)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solutionPath)!, path!)))
                    .Where(File.Exists)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (paths.Length > 0)
                {
                    return paths;
                }
            }
            catch
            {
                // Fall back to filesystem scan below.
            }
        }

        return Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
