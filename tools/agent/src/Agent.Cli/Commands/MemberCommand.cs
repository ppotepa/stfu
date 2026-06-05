using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agent.Cli.Commands;

public static class MemberCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options, SemanticWorkspace? semanticWorkspace = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("member", root, Path.GetRelativePath(root, solution));

        if (options.Get("symbol-id") is { Length: > 0 } symbolId)
        {
            try
            {
                await AddBySymbolIdAsync(root, config, options, envelope, symbolId, semanticWorkspace);
            }
            catch (Exception ex)
            {
                envelope.Warnings.Add($"semantic member unavailable: {ex.Message}");
            }
        }

        if (envelope.Items.Count == 0)
        {
            if (options.FromStdin)
            {
                foreach (var stdinSymbolId in StdinItems.ReadSymbolIds().Take(options.Max))
                {
                    try
                    {
                        await AddBySymbolIdAsync(root, config, options, envelope, stdinSymbolId, semanticWorkspace);
                    }
                    catch (Exception ex)
                    {
                        envelope.Warnings.Add($"semantic member unavailable for {stdinSymbolId}: {ex.Message}");
                    }
                }
            }

            if (envelope.Items.Count == 0)
            {
                var locations = options.FromStdin
                    ? StdinItems.ReadLocations().Take(options.Max).ToArray()
                    : ReadExplicitLocation(options);

                if (locations.Length == 0)
                {
                    return AgentEnvelope.Failure("member", root, Path.GetRelativePath(root, solution), "missing_location", "Provide --symbol-id or --file <path> --line <n>.");
                }

                foreach (var location in locations)
                {
                    AddMemberByLocation(root, options, envelope, location);
                }

                envelope.Summary.Count = locations.Length;
            }
        }

        if (envelope.Summary.Count == 0) envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }

    private static async Task AddBySymbolIdAsync(string root, AgentConfig config, ToolOptions options, AgentEnvelope envelope, string symbolId, SemanticWorkspace? semanticWorkspace = null)
    {
        var workspace = semanticWorkspace ?? await SemanticWorkspace.LoadAsync(root, config, options);
        var symbol = await SymbolIds.ResolveAsync(workspace.Solution, symbolId);
        if (symbol == null)
        {
            envelope.Warnings.Add($"symbol not found: {symbolId}");
            return;
        }

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences.Take(options.Max))
        {
            var node = await syntaxReference.GetSyntaxAsync();
            var tree = node.SyntaxTree;
            var text = await tree.GetTextAsync();
            var span = node.GetLocation().GetLineSpan();
            var start = Math.Max(1, span.StartLinePosition.Line + 1 - options.Context);
            var end = Math.Min(text.Lines.Count, span.EndLinePosition.Line + 1 + options.Context);
            var code = string.Join(Environment.NewLine, Enumerable.Range(start - 1, end - start + 1).Select(i => text.Lines[i].ToString()));
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = "member",
                ["symbolId"] = symbolId,
                ["displayName"] = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                ["file"] = Path.GetRelativePath(root, tree.FilePath),
                ["line"] = span.StartLinePosition.Line + 1,
                ["span"] = new Dictionary<string, object?> { ["startLine"] = start, ["endLine"] = end },
                ["signature"] = GetSignature(node),
                ["code"] = code,
                ["omitted"] = new Dictionary<string, object?> { ["beforeLines"] = start - 1, ["afterLines"] = text.Lines.Count - end }
            });
        }

        envelope.Summary.Count = envelope.Items.Count;
    }

    private static string GetSignature(SyntaxNode node)
    {
        return node switch
        {
            MethodDeclarationSyntax method => method.Identifier.Text + method.ParameterList,
            ConstructorDeclarationSyntax ctor => ctor.Identifier.Text + ctor.ParameterList,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            BaseTypeDeclarationSyntax type => type.Kind() + " " + type.Identifier.Text,
            _ => node.ToString().Split(["\r\n", "\n"], StringSplitOptions.None).FirstOrDefault()?.Trim() ?? ""
        };
    }

    private static SourceLocation[] ReadExplicitLocation(ToolOptions options)
    {
        var file = options.Get("file");
        return !string.IsNullOrWhiteSpace(file) && int.TryParse(options.Get("line"), out var line)
            ? [new SourceLocation(file, line)]
            : [];
    }

    private static void AddMemberByLocation(string root, ToolOptions options, AgentEnvelope envelope, SourceLocation location)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, location.File));
        if (!File.Exists(fullPath))
        {
            envelope.Warnings.Add($"file not found: {location.File}");
            return;
        }

        var lines = File.ReadAllLines(fullPath);
        var start = Math.Max(1, FindStart(lines, location.Line) - options.Context);
        var end = Math.Min(lines.Length, FindEnd(lines, location.Line) + options.Context);
        var code = string.Join(Environment.NewLine, lines.Skip(start - 1).Take(end - start + 1));

        envelope.Items.Add(new Dictionary<string, object?>
        {
            ["itemType"] = "member",
            ["file"] = Path.GetRelativePath(root, fullPath),
            ["line"] = location.Line,
            ["span"] = new Dictionary<string, object?> { ["startLine"] = start, ["endLine"] = end },
            ["code"] = code,
            ["omitted"] = new Dictionary<string, object?> { ["beforeLines"] = start - 1, ["afterLines"] = lines.Length - end }
        });
    }

    private static int FindStart(string[] lines, int line)
    {
        for (var i = Math.Min(line - 1, lines.Length - 1); i >= 0; i--)
        {
            var text = lines[i].TrimStart();
            if (text.StartsWith("public ", StringComparison.Ordinal) ||
                text.StartsWith("private ", StringComparison.Ordinal) ||
                text.StartsWith("internal ", StringComparison.Ordinal) ||
                text.StartsWith("protected ", StringComparison.Ordinal) ||
                text.StartsWith("static ", StringComparison.Ordinal) ||
                text.StartsWith("sealed ", StringComparison.Ordinal) ||
                text.StartsWith("partial ", StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return Math.Max(1, line - 20);
    }

    private static int FindEnd(string[] lines, int line)
    {
        var depth = 0;
        var seenBrace = false;
        for (var i = Math.Max(0, line - 1); i < lines.Length; i++)
        {
            foreach (var ch in lines[i])
            {
                if (ch == '{') { depth++; seenBrace = true; }
                if (ch == '}') depth--;
            }

            if (seenBrace && depth <= 0)
            {
                return i + 1;
            }
        }

        return Math.Min(lines.Length, line + 40);
    }
}
