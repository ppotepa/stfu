using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Agent.Cli.Commands;

public static class RefsCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options, SemanticWorkspace? semanticWorkspace = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var solutionPath = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("refs", root, Path.GetRelativePath(root, solutionPath));
        var symbolIds = new List<string>();
        if (options.Get("symbol-id") is { Length: > 0 } explicitId) symbolIds.Add(explicitId);
        if (options.FromStdin) symbolIds.AddRange(StdinItems.ReadSymbolIds());
        if (symbolIds.Count == 0 && options.Get("name") is { Length: > 0 } name)
        {
            var symbols = await SymbolsCommand.ExecuteAsync(root, config, ToolOptions.Parse(["--name", name, "--max", "1", "--format", "json"], "json"));
            symbolIds.AddRange(symbols.Items.Select(item => item.TryGetValue("symbolId", out var id) ? id?.ToString() : null).Where(id => !string.IsNullOrWhiteSpace(id))!);
        }

        if (symbolIds.Count == 0)
        {
            return AgentEnvelope.Failure("refs", root, Path.GetRelativePath(root, solutionPath), "missing_symbol", "Provide --symbol-id, --name, or --from-stdin.");
        }

        if (options.Get("refresh") != "true" && symbolIds.Count == 1 && CacheStore.IsAvailable(root, config, "references"))
        {
            foreach (var item in CacheStore.ReadItems(root, config, "references", ToolOptions.Parse(["--symbol-id", symbolIds[0], "--max", options.Max.ToString()], "json")))
            {
                envelope.Items.Add(item);
            }
            if (envelope.Items.Count > 0)
            {
                envelope.Summary.Count = envelope.Items.Count;
                envelope.Summary.Returned = envelope.Items.Count;
                envelope.Summary.Cache = "warm";
                envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
                return envelope;
            }
        }

        var workspace = semanticWorkspace ?? await SemanticWorkspace.LoadAsync(root, config, options);
        foreach (var symbolId in symbolIds.Distinct().Take(options.Max))
        {
            var symbol = await SymbolIds.ResolveAsync(workspace.Solution, symbolId);
            if (symbol == null)
            {
                envelope.Warnings.Add($"symbol not found: {symbolId}");
                continue;
            }

            var refs = await SymbolFinder.FindReferencesAsync(symbol, workspace.Solution);
            foreach (var reference in refs)
            {
                foreach (var location in reference.Locations)
                {
                    if (!location.Location.IsInSource) continue;
                    var span = location.Location.GetLineSpan();
                    var document = workspace.Solution.GetDocument(location.Document.Id);
                    envelope.Items.Add(new Dictionary<string, object?>
                    {
                        ["itemType"] = "reference",
                        ["symbolId"] = symbolId,
                        ["referencedSymbol"] = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        ["usage"] = "reference",
                        ["project"] = document?.Project.Name,
                        ["file"] = Path.GetRelativePath(root, span.Path),
                        ["line"] = span.StartLinePosition.Line + 1,
                        ["column"] = span.StartLinePosition.Character + 1,
                        ["text"] = location.Location.SourceTree?.GetText().Lines[span.StartLinePosition.Line].ToString().Trim(),
                        ["next"] = new[] { $@".\tools\agent\agent.ps1 member --file ""{Path.GetRelativePath(root, span.Path)}"" --line {span.StartLinePosition.Line + 1}" }
                    });
                    if (envelope.Items.Count >= options.Max) break;
                }
                if (envelope.Items.Count >= options.Max) break;
            }
        }

        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = envelope.Items.Count >= options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        CacheStore.WriteJsonLines(root, config, "references.jsonl", envelope.Items);
        return envelope;
    }
}
