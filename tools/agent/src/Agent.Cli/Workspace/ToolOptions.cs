namespace Agent.Cli.Workspace;

public sealed class ToolOptions
{
    public string Format { get; set; } = "table";
    public int Max { get; set; } = 50;
    public int Context { get; set; } = 0;
    public bool ChangedOnly { get; set; }
    public bool NoRestore { get; set; }
    public bool Literal { get; set; }
    public bool FromStdin { get; set; }
    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(string name) => Values.TryGetValue(name, out var value) ? value : null;

    public static ToolOptions Parse(string[] args, string defaultFormat)
    {
        var options = new ToolOptions { Format = defaultFormat };
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                options.Values[$"arg{i}"] = arg;
                continue;
            }

            var key = arg[2..];
            if (key is "changed-only") { options.ChangedOnly = true; continue; }
            if (key is "no-restore") { options.NoRestore = true; continue; }
            if (key is "literal") { options.Literal = true; continue; }
            if (key is "from-stdin") { options.FromStdin = true; continue; }

            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";

            options.Values[key] = value;
            if (key == "format") options.Format = value;
            if (key == "max" && int.TryParse(value, out var max)) options.Max = Math.Max(1, max);
            if (key == "context" && int.TryParse(value, out var context)) options.Context = Math.Max(0, context);
        }

        return options;
    }
}
