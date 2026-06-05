using System.Text.Json;

namespace Agent.Cli.Workspace;

public sealed record SourceLocation(string File, int Line);

public static class StdinItems
{
    private static string? CachedInput;

    private static string ReadInput()
    {
        return CachedInput ??= Console.In.ReadToEnd();
    }

    public static IReadOnlyList<string> ReadSymbolIds()
    {
        var input = ReadInput();
        if (string.IsNullOrWhiteSpace(input)) return [];

        if (input.TrimStart().StartsWith('{') || input.TrimStart().StartsWith('['))
        {
            using var document = JsonDocument.Parse(input);
            var ids = new List<string>();
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                ReadSymbolIds(items, ids);
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                ReadSymbolIds(document.RootElement, ids);
            }

            return ids;
        }

        return TextLines.Split(input).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

    public static IReadOnlyList<SourceLocation> ReadLocations()
    {
        var input = ReadInput();
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        input = input.Trim();
        if (input.StartsWith('{') || input.StartsWith('['))
        {
            return ReadJsonLocations(input);
        }

        return TextLines.Split(input)
            .Select(line => line.Split(':'))
            .Where(parts => parts.Length >= 2 && int.TryParse(parts[1], out _))
            .Select(parts => new SourceLocation(parts[0], int.Parse(parts[1])))
            .ToArray();
    }

    private static IReadOnlyList<SourceLocation> ReadJsonLocations(string input)
    {
        using var document = JsonDocument.Parse(input);
        var locations = new List<SourceLocation>();
        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("items", out var items) &&
            items.ValueKind == JsonValueKind.Array)
        {
            ReadArray(items, locations);
        }
        else if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            ReadArray(document.RootElement, locations);
        }

        return locations;
    }

    private static void ReadArray(JsonElement items, List<SourceLocation> locations)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("file", out var fileProperty))
            {
                continue;
            }

            var file = fileProperty.GetString();
            var line = 0;
            if (item.TryGetProperty("line", out var lineProperty) && lineProperty.ValueKind == JsonValueKind.Number)
            {
                line = lineProperty.GetInt32();
            }
            else if (item.TryGetProperty("span", out var span) &&
                span.ValueKind == JsonValueKind.Object &&
                span.TryGetProperty("startLine", out var startLine) &&
                startLine.ValueKind == JsonValueKind.Number)
            {
                line = startLine.GetInt32();
            }

            if (!string.IsNullOrWhiteSpace(file) && line > 0)
            {
                locations.Add(new SourceLocation(file, line));
            }
        }
    }

    private static void ReadSymbolIds(JsonElement items, List<string> ids)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("symbolId", out var symbolId) && symbolId.GetString() is { Length: > 0 } id)
            {
                ids.Add(id);
            }
        }
    }
}
