using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Agent.Cli.Workspace;

public sealed class SemanticWorkspace
{
    private SemanticWorkspace(MSBuildWorkspace workspace, Solution solution)
    {
        Workspace = workspace;
        Solution = solution;
    }

    public MSBuildWorkspace Workspace { get; }
    public Solution Solution { get; }

    public static async Task<SemanticWorkspace> LoadAsync(string root, AgentConfig config, ToolOptions options)
    {
        RegisterMsBuild();
        var solutionPath = ProjectScanner.ResolveSolution(root, config, options);
        var workspace = MSBuildWorkspace.Create();
#pragma warning disable CS0618
        workspace.WorkspaceFailed += (_, e) => Console.Error.WriteLine($"msbuild-workspace: {e.Diagnostic.Message}");
#pragma warning restore CS0618
        var solution = await workspace.OpenSolutionAsync(solutionPath);
        return new SemanticWorkspace(workspace, solution);
    }

    private static void RegisterMsBuild()
    {
        if (MSBuildLocator.IsRegistered)
        {
            return;
        }

        try
        {
            MSBuildLocator.RegisterDefaults();
        }
        catch
        {
            if (!MSBuildLocator.IsRegistered)
            {
                throw;
            }
        }
    }
}

public static class SymbolIds
{
    public static string Create(ISymbol symbol)
    {
        var docId = symbol.GetDocumentationCommentId();
        if (!string.IsNullOrWhiteSpace(docId))
        {
            return docId;
        }

        var location = symbol.Locations.FirstOrDefault(candidate => candidate.IsInSource);
        if (location != null)
        {
            var span = location.GetLineSpan();
            var file = span.Path.Replace('\\', '/');
            return $"loc:{file}:{span.StartLinePosition.Line + 1}:{span.StartLinePosition.Character + 1}:{symbol.Name}";
        }

        return $"symbol:{symbol.Kind}:{symbol.ContainingNamespace}.{symbol.Name}";
    }

    public static async Task<ISymbol?> ResolveAsync(Solution solution, string symbolId)
    {
        if (symbolId.StartsWith("loc:", StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveLocationSymbolAsync(solution, symbolId);
        }

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(symbolId, compilation);
            if (symbol != null) return symbol;
        }

        return null;
    }

    private static async Task<ISymbol?> ResolveLocationSymbolAsync(Solution solution, string symbolId)
    {
        var parts = symbolId.Split(':');
        if (parts.Length < 5 || !int.TryParse(parts[^3], out var line) || !int.TryParse(parts[^2], out var column))
        {
            return null;
        }

        var file = string.Join(':', parts.Skip(1).Take(parts.Length - 4)).Replace('/', Path.DirectorySeparatorChar);
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath == null || !document.FilePath.Replace('\\', Path.DirectorySeparatorChar).EndsWith(file, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var root = await document.GetSyntaxRootAsync();
                var model = await document.GetSemanticModelAsync();
                var text = await document.GetTextAsync();
                if (root == null || model == null || line <= 0 || line > text.Lines.Count) continue;
                var position = text.Lines[line - 1].Start + Math.Max(0, column - 1);
                var node = root.FindToken(position).Parent?.AncestorsAndSelf().FirstOrDefault(candidate => model.GetDeclaredSymbol(candidate) != null);
                if (node != null) return model.GetDeclaredSymbol(node);
            }
        }

        return null;
    }
}

public static class SymbolItems
{
    public static Dictionary<string, object?> Create(string root, Document document, ISymbol symbol, Location location, double rank = 1.0)
    {
        var span = location.GetLineSpan();
        var file = span.Path;
        if (Path.IsPathRooted(file))
        {
            file = Path.GetRelativePath(root, file);
        }

        return new Dictionary<string, object?>
        {
            ["itemType"] = "symbol",
            ["symbolId"] = SymbolIds.Create(symbol),
            ["kind"] = Kind(symbol),
            ["name"] = symbol.Name,
            ["displayName"] = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            ["project"] = document.Project.Name,
            ["file"] = file,
            ["line"] = span.StartLinePosition.Line + 1,
            ["column"] = span.StartLinePosition.Character + 1,
            ["rank"] = rank,
            ["next"] = new[]
            {
                $@".\tools\agent\agent.ps1 member --symbol-id ""{SymbolIds.Create(symbol)}""",
                $@".\tools\agent\agent.ps1 refs --symbol-id ""{SymbolIds.Create(symbol)}"""
            }
        };
    }

    public static string Kind(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol named when named.IsRecord => "record",
        INamedTypeSymbol named when named.TypeKind == TypeKind.Class => "class",
        INamedTypeSymbol named when named.TypeKind == TypeKind.Interface => "interface",
        INamedTypeSymbol named when named.TypeKind == TypeKind.Struct => "struct",
        INamedTypeSymbol named when named.TypeKind == TypeKind.Enum => "enum",
        IMethodSymbol method when method.MethodKind == MethodKind.Constructor => "ctor",
        IMethodSymbol => "method",
        IPropertySymbol => "property",
        IFieldSymbol => "field",
        IEventSymbol => "event",
        INamespaceSymbol => "namespace",
        _ => symbol.Kind.ToString().ToLowerInvariant()
    };
}
