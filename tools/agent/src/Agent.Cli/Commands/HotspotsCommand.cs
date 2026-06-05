using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agent.Cli.Commands;

public static class HotspotsCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("hotspots", root, Path.GetRelativePath(root, solution));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowedRules = (options.Get("rules") ?? options.Get("rule") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var files = options.FromStdin
            ? StdinItems.ReadLocations().Select(location => Path.Combine(root, location.File)).Where(File.Exists).Distinct().ToArray()
            : Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .ToArray();

        foreach (var file in files)
        {
            foreach (var hotspot in AnalyzeFile(root, file).Where(item => RuleAllowed(item, allowedRules)))
            {
                var key = $"{hotspot["rule"]}|{hotspot["file"]}|{hotspot["line"]}|{string.Join(",", hotspot["evidence"] as string[] ?? [])}";
                if (!seen.Add(key)) continue;
                envelope.Items.Add(hotspot);
                if (envelope.Items.Count >= options.Max) break;
            }
            if (envelope.Items.Count >= options.Max) break;
        }

        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = envelope.Items.Count >= options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }

    private static IEnumerable<Dictionary<string, object?>> AnalyzeFile(string root, string file)
    {
        SyntaxTree tree;
        try { tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file); }
        catch { yield break; }
        var rootNode = tree.GetRoot();

        foreach (var loop in rootNode.DescendantNodes().Where(node => node is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax))
        {
            foreach (var allocation in loop.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                yield return Item(root, file, allocation.GetLocation(), "alloc-in-loop", 0.86, "ObjectCreationExpression inside loop", EnclosingMember(allocation));
            foreach (var invocation in loop.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsLinqLike))
                yield return Item(root, file, invocation.GetLocation(), "linq-in-loop", 0.78, "LINQ-like invocation inside loop", EnclosingMember(invocation));
        }

        foreach (var invocation in rootNode.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var text = invocation.ToString();
            if (text.Contains(".Wait(", StringComparison.Ordinal) || text.Contains("GetAwaiter().GetResult()", StringComparison.Ordinal))
                yield return Item(root, file, invocation.GetLocation(), "sync-wait", 0.88, "sync-over-async invocation", EnclosingMember(invocation));
        }

        foreach (var access in rootNode.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Name.Identifier.Text == "Result")
                yield return Item(root, file, access.GetLocation(), "sync-wait", 0.84, "Task.Result-like member access", EnclosingMember(access));
        }

        foreach (var attribute in rootNode.DescendantNodes().OfType<AttributeSyntax>())
        {
            var name = attribute.Name.ToString();
            if (name.Contains("DllImport", StringComparison.OrdinalIgnoreCase) || name.Contains("LibraryImport", StringComparison.OrdinalIgnoreCase))
                yield return Item(root, file, attribute.GetLocation(), "pinvoke", 0.75, "interop boundary attribute", EnclosingMember(attribute));
        }

        foreach (var unsafeNode in rootNode.DescendantNodes().Where(node => node is UnsafeStatementSyntax or PointerTypeSyntax))
            yield return Item(root, file, unsafeNode.GetLocation(), "unsafe", 0.68, "unsafe syntax", EnclosingMember(unsafeNode));

        foreach (var method in rootNode.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
        {
            var span = method.GetLocation().GetLineSpan();
            var lines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
            if (lines > 120)
                yield return Item(root, file, method.GetLocation(), "large-method", 0.55, $"method spans {lines} lines", EnclosingMember(method));
        }
    }

    private static bool IsLinqLike(InvocationExpressionSyntax invocation)
    {
        var text = invocation.Expression.ToString();
        return text.Contains(".Select", StringComparison.Ordinal) ||
               text.Contains(".Where", StringComparison.Ordinal) ||
               text.Contains(".ToArray", StringComparison.Ordinal) ||
               text.Contains(".ToList", StringComparison.Ordinal) ||
               text.Contains("Enumerable.", StringComparison.Ordinal);
    }

    private static bool RuleAllowed(Dictionary<string, object?> item, HashSet<string> allowedRules)
    {
        return allowedRules.Count == 0 || allowedRules.Contains(item["rule"]?.ToString() ?? "");
    }

    private static string EnclosingMember(SyntaxNode node)
    {
        var member = node.AncestorsAndSelf().FirstOrDefault(candidate => candidate is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or BaseTypeDeclarationSyntax);
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.Text,
            ConstructorDeclarationSyntax ctor => ctor.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            BaseTypeDeclarationSyntax type => type.Identifier.Text,
            _ => ""
        };
    }

    private static Dictionary<string, object?> Item(string root, string file, Location location, string rule, double score, string evidence, string member)
    {
        var span = location.GetLineSpan();
        var line = span.StartLinePosition.Line + 1;
        return new Dictionary<string, object?>
        {
            ["itemType"] = "hotspot",
            ["rule"] = rule,
            ["severity"] = score > 0.8 ? "warning" : "info",
            ["score"] = score,
            ["file"] = Path.GetRelativePath(root, file),
            ["line"] = line,
            ["member"] = member,
            ["evidence"] = new[] { evidence },
            ["next"] = new[] { $@".\tools\agent\agent.ps1 member --file ""{Path.GetRelativePath(root, file)}"" --line {line}" }
        };
    }
}
