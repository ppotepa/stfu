using System.Diagnostics;
using System.Text.RegularExpressions;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agent.Cli.Commands;

public static partial class DiffCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("diff", root, Path.GetRelativePath(root, solution));
        var status = TextLines.Split((await ProcessRunner.RunAsync("git", ["status", "--porcelain"], root)).Stdout);
        var projectItems = ProjectsCommand.Execute(root, config, ToolOptions.Parse(["--max", "10000"], "json")).Items;

        var semantic = options.Get("semantic") == "true";
        foreach (var line in status.Take(options.Max))
        {
            var code = line.Length >= 2 ? line[..2] : "??";
            var file = line.Length > 3 ? line[3..] : line;
            var item = new Dictionary<string, object?>
            {
                ["itemType"] = "change",
                ["changeKind"] = Kind(code),
                ["file"] = file,
                ["language"] = Language(file),
                ["status"] = code.Trim()
            };

            if (semantic && file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                AddSemanticChangeInfo(root, file, item);
            }
            AddProjectImpact(file, item, projectItems);

            envelope.Items.Add(item);
        }

        envelope.Summary.Count = status.Length;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = status.Length > options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        envelope.Next.Add(@".\tools\agent\agent.ps1 symbols --changed-only --format json");
        return envelope;
    }

    private static string Kind(string code)
    {
        if (code.Contains("A", StringComparison.Ordinal)) return "added";
        if (code.Contains("D", StringComparison.Ordinal)) return "deleted";
        if (code.Contains("R", StringComparison.Ordinal)) return "renamed";
        if (code.Contains("?", StringComparison.Ordinal)) return "untracked";
        return "modified";
    }

    private static string Language(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".cs" => "csharp",
        ".csproj" => "msbuild",
        ".json" => "json",
        ".ps1" => "powershell",
        ".md" => "markdown",
        _ => "text"
    };

    private static void AddSemanticChangeInfo(string root, string file, Dictionary<string, object?> item)
    {
        var path = Path.Combine(root, file);
        if (!File.Exists(path)) return;
        var hunks = ReadHunks(root, file);
        var changedLine = hunks.FirstOrDefault()?.NewStart ?? 0;
        if (changedLine <= 0) return;

        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
        var rootNode = tree.GetRoot();
        var text = tree.GetText();
        var position = text.Lines[Math.Min(changedLine - 1, text.Lines.Count - 1)].Start;
        var node = rootNode.FindToken(position).Parent;
        var member = node?.AncestorsAndSelf().FirstOrDefault(candidate =>
            candidate is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or BaseTypeDeclarationSyntax);
        if (member == null) return;

        var span = member.GetLocation().GetLineSpan();
        item["member"] = Name(member);
        item["line"] = changedLine;
        item["hunks"] = hunks.Select(hunk => new Dictionary<string, object?>
        {
            ["oldStart"] = hunk.OldStart,
            ["oldLines"] = hunk.OldLines,
            ["newStart"] = hunk.NewStart,
            ["newLines"] = hunk.NewLines
        }).ToArray();
        item["span"] = new Dictionary<string, object?>
        {
            ["startLine"] = span.StartLinePosition.Line + 1,
            ["endLine"] = span.EndLinePosition.Line + 1
        };
    }

    private static IReadOnlyList<DiffHunk> ReadHunks(string root, string file)
    {
        var result = ProcessRunner.RunAsync("git", ["diff", "-U0", "--", file], root).GetAwaiter().GetResult();
        var hunks = new List<DiffHunk>();
        foreach (var line in TextLines.Split(result.Stdout))
        {
            var match = HunkRegex().Match(line);
            if (match.Success)
            {
                hunks.Add(new DiffHunk(
                    ParseInt(match.Groups["oldStart"].Value),
                    ParseOptionalCount(match.Groups["oldLines"].Value),
                    ParseInt(match.Groups["newStart"].Value),
                    ParseOptionalCount(match.Groups["newLines"].Value)));
            }
        }

        return hunks;
    }

    private static void AddProjectImpact(string file, Dictionary<string, object?> item, List<Dictionary<string, object?>> projectItems)
    {
        var normalized = file.Replace('\\', '/');
        var containing = projectItems.FirstOrDefault(project =>
            normalized.StartsWith((Path.GetDirectoryName(project["path"]?.ToString() ?? "") ?? "").Replace('\\', '/').TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase));
        if (containing == null) return;
        var projectName = containing["project"]?.ToString() ?? "";
        item["project"] = projectName;
        var affected = ProjectsCommand.Execute(Directory.GetCurrentDirectory(), AgentConfig.Load(Directory.GetCurrentDirectory()), ToolOptions.Parse(["--changed-only", "--affected", "--max", "10000"], "json"))
            .Items.Count(project => project.TryGetValue("affectedByChangedFiles", out var value) && value is true);
        item["risk"] = new Dictionary<string, object?> { ["affectedProjects"] = affected, ["score"] = affected > 0 ? Math.Min(1.0, affected / 10.0) : 0 };
    }

    private static string Name(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax method => method.Identifier.Text,
        ConstructorDeclarationSyntax ctor => ctor.Identifier.Text,
        PropertyDeclarationSyntax property => property.Identifier.Text,
        BaseTypeDeclarationSyntax type => type.Identifier.Text,
        _ => node.Kind().ToString()
    };

    private static int ParseInt(string text) => int.TryParse(text, out var value) ? value : 0;
    private static int ParseOptionalCount(string text) => int.TryParse(text, out var value) ? value : 1;

    private sealed record DiffHunk(int OldStart, int OldLines, int NewStart, int NewLines);

    [GeneratedRegex(@"@@ -(?<oldStart>\d+)(?:,(?<oldLines>\d+))? \+(?<newStart>\d+)(?:,(?<newLines>\d+))? @@")]
    private static partial Regex HunkRegex();
}
