using System.Text.Json;
using Agent.Cli.Protocol;

namespace Agent.Cli.Formatting;

public static class Formatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Write(AgentEnvelope envelope, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                Console.WriteLine(JsonSerializer.Serialize(envelope, JsonOptions));
                break;
            case "ndjson":
                foreach (var item in envelope.Items)
                {
                    Console.WriteLine(JsonSerializer.Serialize(item, JsonOptions));
                }
                break;
            case "markdown":
                WriteMarkdown(envelope);
                break;
            default:
                WriteTable(envelope);
                break;
        }
    }

    private static void WriteMarkdown(AgentEnvelope envelope)
    {
        Console.WriteLine($"# {envelope.Tool}");
        Console.WriteLine();
        Console.WriteLine($"count: {envelope.Summary.Count}; returned: {envelope.Summary.Returned}; truncated: {envelope.Summary.Truncated.ToString().ToLowerInvariant()}");
        Console.WriteLine();
        foreach (var item in envelope.Items)
        {
            Console.WriteLine("- " + string.Join("; ", item.Take(8).Select(pair => $"{pair.Key}: {pair.Value}")));
        }
    }

    private static void WriteTable(AgentEnvelope envelope)
    {
        if (!envelope.Ok)
        {
            Console.WriteLine($"{envelope.Error?.Code}\t{envelope.Error?.Message}");
            return;
        }

        if (envelope.Items.Count == 0)
        {
            Console.WriteLine($"{envelope.Tool}: no items");
            return;
        }

        var keys = envelope.Items
            .SelectMany(item => item.Keys)
            .Where(key => key is "itemType" or "name" or "kind" or "project" or "file" or "line" or "status" or "code" or "severity" or "message" or "displayName" or "changeKind")
            .Distinct()
            .Take(6)
            .ToArray();

        Console.WriteLine(string.Join("\t", keys));
        foreach (var item in envelope.Items)
        {
            Console.WriteLine(string.Join("\t", keys.Select(key => item.TryGetValue(key, out var value) ? value?.ToString() : "")));
        }
    }
}
