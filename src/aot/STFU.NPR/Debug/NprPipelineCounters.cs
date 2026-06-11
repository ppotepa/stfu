using System;
using System.Collections.Generic;
using System.Text;

namespace STFU.NPR.Debug;

public sealed class NprPipelineCounters
{
    public readonly Dictionary<string, long> Values = new(StringComparer.Ordinal);

    public void Clear() => Values.Clear();

    public void Set(string name, long value)
    {
        Values[name] = value;
    }

    public void Add(string name, long delta)
    {
        Values.TryGetValue(name, out var old);
        Values[name] = old + delta;
    }

    public bool TryGet(string name, out long value)
    {
        return Values.TryGetValue(name, out value);
    }

    public IReadOnlyDictionary<string, long> Snapshot()
    {
        return new Dictionary<string, long>(Values, StringComparer.Ordinal);
    }

    public void SetMax(string name, long value)
    {
        Values.TryGetValue(name, out var old);
        if (value > old)
        {
            Values[name] = value;
        }
    }

    public void SetMin(string name, long value)
    {
        if (!Values.TryGetValue(name, out var old) || value < old)
        {
            Values[name] = value;
        }
    }

    public string FormatStep(string prefix)
    {
        if (Values.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(256);
        foreach (var pair in Values)
        {
            if (!pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(pair.Key.AsSpan(prefix.Length));
            builder.Append('=');
            builder.Append(pair.Value);
        }

        return builder.ToString();
    }
}
