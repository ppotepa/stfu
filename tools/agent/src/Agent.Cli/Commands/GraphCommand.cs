using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agent.Cli.Commands;

public static class GraphCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options, SemanticWorkspace? semanticWorkspace = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var solutionPath = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("graph", root, Path.GetRelativePath(root, solutionPath));
        var kind = options.Get("kind") ?? "call";
        if (options.Get("refresh") != "true" && kind != "project" && CacheStore.IsAvailable(root, config, "callgraph"))
        {
            foreach (var edge in CacheStore.ReadItems(root, config, "callgraph", options))
            {
                envelope.Edges.Add(edge);
                if (envelope.Edges.Count >= options.Max) break;
            }
            if (envelope.Edges.Count > 0)
            {
                envelope.Summary.Count = envelope.Edges.Count;
                envelope.Summary.Returned = envelope.Edges.Count;
                envelope.Summary.Cache = "warm";
                envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
                return envelope;
            }
        }

        if (kind.Equals("project", StringComparison.OrdinalIgnoreCase) || options.Get("root")?.Equals("projects", StringComparison.OrdinalIgnoreCase) == true)
        {
            AddProjectGraph(root, config, options, envelope, solutionPath);
        }
        else
        {
            await AddCallGraphAsync(root, config, options, envelope, semanticWorkspace);
        }

        envelope.Summary.Count = envelope.Edges.Count;
        envelope.Summary.Returned = envelope.Edges.Count;
        envelope.Summary.Truncated = envelope.Edges.Count >= options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        CacheStore.WriteJsonLines(root, config, "callgraph.jsonl", envelope.Edges);
        return envelope;
    }

    private static void AddProjectGraph(string root, AgentConfig config, ToolOptions options, AgentEnvelope envelope, string solutionPath)
    {
        foreach (var projectPath in ProjectScanner.FindProjects(root, solutionPath))
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var item = ProjectsCommand.ReadProjectItem(root, projectPath);
            envelope.Items.Add(item);
            if (item.TryGetValue("projectReferences", out var references) && references is string[] projectReferences)
            {
                foreach (var reference in projectReferences)
                {
                    envelope.Edges.Add(new Dictionary<string, object?>
                    {
                        ["edgeType"] = "projectReference",
                        ["from"] = projectName,
                        ["to"] = reference
                    });
                    if (envelope.Edges.Count >= options.Max) return;
                }
            }
        }
    }

    private static async Task AddCallGraphAsync(string root, AgentConfig config, ToolOptions options, AgentEnvelope envelope, SemanticWorkspace? semanticWorkspace)
    {
        var rootId = options.Get("root") ?? options.Get("symbol-id");
        if (string.IsNullOrWhiteSpace(rootId) && options.FromStdin)
        {
            rootId = StdinItems.ReadSymbolIds().FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(rootId))
        {
            envelope.Warnings.Add("Provide --root <symbolId> or pipe symbols into graph --from-stdin.");
            return;
        }

        var workspace = semanticWorkspace ?? await SemanticWorkspace.LoadAsync(root, config, options);
        var symbol = await SymbolIds.ResolveAsync(workspace.Solution, rootId);
        if (symbol == null)
        {
            envelope.Warnings.Add($"symbol not found: {rootId}");
            return;
        }

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var node = await syntaxReference.GetSyntaxAsync();
            var document = workspace.Solution.GetDocument(node.SyntaxTree);
            if (document == null) continue;
            var model = await document.GetSemanticModelAsync();
            if (model == null) continue;

            foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var target = model.GetSymbolInfo(invocation).Symbol;
                if (target == null) continue;
                var span = invocation.GetLocation().GetLineSpan();
                envelope.Edges.Add(new Dictionary<string, object?>
                {
                    ["edgeType"] = "call",
                    ["fromSymbolId"] = rootId,
                    ["from"] = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    ["toSymbolId"] = SymbolIds.Create(target),
                    ["to"] = target.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    ["file"] = Path.GetRelativePath(root, span.Path),
                    ["line"] = span.StartLinePosition.Line + 1,
                    ["confidence"] = "semantic",
                    ["dispatch"] = target.IsVirtual || target.IsAbstract ? "virtual" : "direct"
                });
                if (envelope.Edges.Count >= options.Max) return;
            }
        }
    }
}
