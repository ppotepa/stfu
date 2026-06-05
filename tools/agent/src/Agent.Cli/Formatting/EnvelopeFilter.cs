using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Formatting;

public static class EnvelopeFilter
{
    public static AgentEnvelope Apply(AgentEnvelope envelope, ToolOptions options)
    {
        ApplyWhere(envelope, options.Get("where"));
        ApplySelect(envelope, options.Get("select"));
        ApplyEmit(envelope, options.Get("emit"));
        return envelope;
    }

    private static void ApplyWhere(AgentEnvelope envelope, string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return;
        var op = expression.Contains('~') ? "~" : expression.Contains('>') ? ">" : expression.Contains('<') ? "<" : "=";
        var parts = expression.Split(op, 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return;
        envelope.Items.RemoveAll(item => !Matches(item, parts[0], op, parts[1]));
        envelope.Summary.Returned = envelope.Items.Count;
    }

    private static bool Matches(Dictionary<string, object?> item, string field, string op, string expected)
    {
        if (!item.TryGetValue(field, out var value) || value == null) return false;
        var text = value.ToString() ?? "";
        if (op == "~") return text.Contains(expected, StringComparison.OrdinalIgnoreCase);
        if (op == "=") return text.Equals(expected, StringComparison.OrdinalIgnoreCase);
        if (double.TryParse(text, out var number) && double.TryParse(expected, out var threshold))
        {
            return op == ">" ? number > threshold : number < threshold;
        }
        return false;
    }

    private static void ApplySelect(AgentEnvelope envelope, string? select)
    {
        if (string.IsNullOrWhiteSpace(select)) return;
        var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < envelope.Items.Count; i++)
        {
            envelope.Items[i] = envelope.Items[i]
                .Where(pair => fields.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void ApplyEmit(AgentEnvelope envelope, string? emit)
    {
        if (string.IsNullOrWhiteSpace(emit) || emit.Equals("all", StringComparison.OrdinalIgnoreCase)) return;
        if (!emit.Contains("items", StringComparison.OrdinalIgnoreCase)) envelope.Items.Clear();
        if (!emit.Contains("edges", StringComparison.OrdinalIgnoreCase)) envelope.Edges.Clear();
        if (!emit.Contains("diagnostics", StringComparison.OrdinalIgnoreCase)) envelope.Diagnostics.Clear();
        if (!emit.Contains("metrics", StringComparison.OrdinalIgnoreCase)) envelope.Metrics.Clear();
        if (!emit.Contains("summary", StringComparison.OrdinalIgnoreCase)) envelope.Summary.Returned = envelope.Items.Count;
    }
}
