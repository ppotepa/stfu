using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agent.Cli.Workspace;

public static class SymbolIndexBuilder
{
    public static async Task<IReadOnlyList<Dictionary<string, object?>>> BuildAsync(string root, Solution solution, int max = 100000)
    {
        var items = new List<Dictionary<string, object?>>();
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents.Where(document => document.FilePath?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true))
            {
                var rootNode = await document.GetSyntaxRootAsync();
                var model = await document.GetSemanticModelAsync();
                if (rootNode == null || model == null) continue;
                foreach (var node in rootNode.DescendantNodes().Where(IsDeclarationNode))
                {
                    var symbol = model.GetDeclaredSymbol(node);
                    if (symbol == null) continue;
                    var location = symbol.Locations.FirstOrDefault(candidate => candidate.IsInSource);
                    if (location == null) continue;
                    items.Add(SymbolItems.Create(root, document, symbol, location, 1.0));
                    if (items.Count >= max) return items;
                }
            }
        }
        return items;
    }

    private static bool IsDeclarationNode(SyntaxNode node)
    {
        return node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or MethodDeclarationSyntax or ConstructorDeclarationSyntax
            or PropertyDeclarationSyntax or FieldDeclarationSyntax or EventDeclarationSyntax or EventFieldDeclarationSyntax;
    }
}
