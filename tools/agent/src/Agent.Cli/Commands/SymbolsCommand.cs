using System.Diagnostics;
using System.Text.RegularExpressions;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agent.Cli.Commands;

public static partial class SymbolsCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options, SemanticWorkspace? semanticWorkspace = null, IReadOnlyList<Dictionary<string, object?>>? runtimeSymbolIndex = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("symbols", root, Path.GetRelativePath(root, solution));
        var name = options.Get("name") ?? options.Get("arg0") ?? "";
        var kind = options.Get("kind");
        envelope.Query["name"] = name;
        envelope.Query["kind"] = kind;

        if (TryLoadRuntimeIndex(options, envelope, runtimeSymbolIndex, name, kind))
        {
            envelope.Summary.Count = envelope.Items.Count;
            envelope.Summary.Returned = envelope.Items.Count;
            envelope.Summary.Cache = "server-memory";
            envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
            return envelope;
        }

        if (TryLoadCached(root, config, options, envelope, name, kind))
        {
            envelope.Summary.Count = envelope.Items.Count;
            envelope.Summary.Returned = envelope.Items.Count;
            envelope.Summary.Cache = "warm";
            envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
            return envelope;
        }

        try
        {
            var workspace = semanticWorkspace ?? await SemanticWorkspace.LoadAsync(root, config, options);
            await AddSemanticSymbolsAsync(root, options, envelope, workspace.Solution, name, kind);
            envelope.Summary.Cache = "cold";
        }
        catch (Exception ex)
        {
            envelope.Warnings.Add($"semantic symbols unavailable, using syntax fallback: {ex.Message}");
            AddSyntaxSymbols(root, options, envelope, name, kind);
            envelope.Summary.Cache = "fallback";
        }

        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = envelope.Items.Count >= options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        CacheStore.WriteJsonLines(root, config, "symbols.jsonl", envelope.Items);
        return envelope;
    }

    private static bool TryLoadRuntimeIndex(ToolOptions options, AgentEnvelope envelope, IReadOnlyList<Dictionary<string, object?>>? index, string name, string? kind)
    {
        if (index is null || index.Count == 0) return false;
        foreach (var item in index)
        {
            if (!string.IsNullOrWhiteSpace(kind) && !StringValue(item, "kind").Equals(kind, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(options.Get("project")) &&
                !StringValue(item, "project").Contains(options.Get("project")!, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(name) &&
                !StringValue(item, "name").Contains(name, StringComparison.OrdinalIgnoreCase) &&
                !StringValue(item, "displayName").Contains(name, StringComparison.OrdinalIgnoreCase)) continue;
            envelope.Items.Add(item);
            if (envelope.Items.Count >= options.Max) break;
        }
        return envelope.Items.Count > 0 || string.IsNullOrWhiteSpace(name);
    }

    private static bool TryLoadCached(string root, AgentConfig config, ToolOptions options, AgentEnvelope envelope, string name, string? kind)
    {
        if (options.Get("refresh") == "true") return false;
        if (!CacheStore.IsAvailable(root, config, "symbols")) return false;
        if (options.Get("cache") != "warm" && string.IsNullOrWhiteSpace(name)) return false;

        var cacheOptions = ToolOptions.Parse(["--max", "100000"], "json");
        foreach (var item in CacheStore.ReadItems(root, config, "symbols", cacheOptions))
        {
            if (!string.IsNullOrWhiteSpace(kind) && !StringValue(item, "kind").Equals(kind, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(name) &&
                !StringValue(item, "name").Contains(name, StringComparison.OrdinalIgnoreCase) &&
                !StringValue(item, "displayName").Contains(name, StringComparison.OrdinalIgnoreCase)) continue;
            envelope.Items.Add(item);
            if (envelope.Items.Count >= options.Max) break;
        }

        return envelope.Items.Count > 0;
    }

    private static string StringValue(Dictionary<string, object?> item, string key)
    {
        return item.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
    }

    private static async Task AddSemanticSymbolsAsync(string root, ToolOptions options, AgentEnvelope envelope, Solution solution, string name, string? kind)
    {
        foreach (var project in solution.Projects)
        {
            if (options.Get("project") is { Length: > 0 } projectFilter &&
                !project.Name.Contains(projectFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var document in project.Documents.Where(document => document.FilePath?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true))
            {
                var rootNode = await document.GetSyntaxRootAsync();
                var model = await document.GetSemanticModelAsync();
                if (rootNode == null || model == null) continue;

                foreach (var node in rootNode.DescendantNodes().Where(IsDeclarationNode))
                {
                    var symbol = model.GetDeclaredSymbol(node);
                    if (symbol == null) continue;
                    var symbolKind = SymbolItems.Kind(symbol);
                    if (!string.IsNullOrWhiteSpace(kind) && !symbolKind.Equals(kind, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(name) && !symbol.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) continue;
                    var location = symbol.Locations.FirstOrDefault(location => location.IsInSource);
                    if (location == null) continue;

                    envelope.Items.Add(SymbolItems.Create(root, document, symbol, location, Rank(symbol.Name, name)));
                    if (envelope.Items.Count >= options.Max) return;
                }
            }
        }
    }

    private static bool IsDeclarationNode(SyntaxNode node)
    {
        return node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or MethodDeclarationSyntax or ConstructorDeclarationSyntax
            or PropertyDeclarationSyntax or FieldDeclarationSyntax or EventDeclarationSyntax or EventFieldDeclarationSyntax;
    }

    private static double Rank(string symbolName, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return 0.5;
        if (symbolName.Equals(query, StringComparison.Ordinal)) return 1.0;
        if (symbolName.Equals(query, StringComparison.OrdinalIgnoreCase)) return 0.95;
        return 0.6;
    }

    private static void AddSyntaxSymbols(string root, ToolOptions options, AgentEnvelope envelope, string name, string? kind)
    {
        var sourceRoot = Path.Combine(root, "src");
        if (!Directory.Exists(sourceRoot)) return;
        var files = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var lineNo = 0;
            foreach (var line in File.ReadLines(file))
            {
                lineNo++;
                var match = SymbolRegex().Match(line);
                if (!match.Success) continue;
                var symbolKind = NormalizeKind(match.Groups["kind"].Value);
                var symbolName = match.Groups["name"].Value;
                if (!string.IsNullOrWhiteSpace(kind) && !symbolKind.Equals(kind, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(name) && !symbolName.Contains(name, StringComparison.OrdinalIgnoreCase)) continue;

                envelope.Items.Add(new Dictionary<string, object?>
                {
                    ["itemType"] = "symbol",
                    ["kind"] = symbolKind,
                    ["name"] = symbolName,
                    ["displayName"] = symbolName,
                    ["file"] = Path.GetRelativePath(root, file),
                    ["line"] = lineNo,
                    ["symbolId"] = $"syntax:{Path.GetRelativePath(root, file)}:{lineNo}:{symbolName}",
                    ["rank"] = symbolName.Equals(name, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.6
                });

                if (envelope.Items.Count >= options.Max) return;
            }
        }
    }

    private static string NormalizeKind(string kind) => kind switch
    {
        "class" or "record" or "struct" or "interface" or "enum" => kind,
        _ => "method"
    };

    [GeneratedRegex(@"\b(?:(public|private|internal|protected|static|sealed|abstract|partial|async|readonly)\s+)*(?<kind>class|record|struct|interface|enum|[A-Za-z0-9_<>,\[\]\?]+\s+)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:\(|[:<{])")]
    private static partial Regex SymbolRegex();
}
