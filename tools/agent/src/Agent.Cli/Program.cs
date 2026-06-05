using Agent.Cli.Commands;
using Agent.Cli.Formatting;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;
using System.Text.Json;

var command = args.Length > 0 ? args[0] : "help";
var commandArgs = args.Skip(1).ToArray();
var root = RepositoryRoot.Find(Environment.CurrentDirectory);
var config = AgentConfig.Load(root);
var options = ToolOptions.Parse(commandArgs, config.DefaultFormat);

if (command is "-h" or "--help" or "help")
{
    HelpCommand.Write();
    return 0;
}

if (command.Equals("server-daemon", StringComparison.OrdinalIgnoreCase))
{
    return await ServerDaemon.RunAsync(root, config, options);
}

AgentEnvelope envelope;
try
{
    envelope = await TryExecuteViaServerAsync(command, commandArgs, root, config, options)
        ?? await CommandDispatcher.ExecuteAsync(command, root, config, options);
}
catch (Exception ex)
{
    envelope = AgentEnvelope.Failure(command, root, config.DefaultSolution, "internal_error", ex.Message);
}

envelope = EnvelopeFilter.Apply(envelope, options);
if (options.Get("next") == "true")
{
    foreach (var next in envelope.Next)
    {
        Console.WriteLine(next);
    }
    return envelope.ToolExitCode;
}

var formatted = FormatEnvelope(envelope, options.Format);
formatted = ApplyBudget(formatted, options);

if (options.Get("out") is { Length: > 0 } outPath)
{
    File.WriteAllText(Path.GetFullPath(Path.Combine(root, outPath)), formatted);
}
else
{
    Console.Write(formatted);
}
if (!envelope.Ok)
{
    Console.Error.WriteLine($"{command}: {envelope.Error?.Code}: {envelope.Error?.Message}");
    return 1;
}

return envelope.ToolExitCode;

static async Task<AgentEnvelope?> TryExecuteViaServerAsync(string command, string[] args, string root, AgentConfig config, ToolOptions options)
{
    if (options.Get("no-server") == "true") return null;
    if (options.Get("use-server") != "true") return null;
    if (!ServerEligible(command)) return null;
    var statePath = ServerDaemon.StatePath(root, config);
    if (!File.Exists(statePath)) return null;

    try
    {
        var state = JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(statePath)) ?? [];
        if (!state.TryGetValue("pipeName", out var pipeValue) || pipeValue == null) return null;
        var pipeName = pipeValue.ToString();
        if (string.IsNullOrWhiteSpace(pipeName)) return null;
        var request = JsonSerializer.Serialize(new ServerRequest { Command = command, Args = args });
        var response = await ServerDaemon.SendAsync(pipeName, request, 750);
        var json = JsonSerializer.Serialize(response);
        return JsonSerializer.Deserialize<AgentEnvelope>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch
    {
        return null;
    }
}

static bool ServerEligible(string command)
{
    return command.ToLowerInvariant() is "symbols" or "refs" or "member" or "graph" or "projects" or "search";
}

static string FormatEnvelope(AgentEnvelope envelope, string format)
{
    using var writer = new StringWriter();
    var previous = Console.Out;
    try
    {
        Console.SetOut(writer);
        Formatter.Write(envelope, format);
        return writer.ToString();
    }
    finally
    {
        Console.SetOut(previous);
    }
}

static string ApplyBudget(string text, ToolOptions options)
{
    if (int.TryParse(options.Get("budget-tokens"), out var tokens))
    {
        var charBudget = Math.Max(0, tokens * 4);
        if (text.Length > charBudget)
        {
            text = text[..charBudget] + Environment.NewLine + "[truncated by --budget-tokens]" + Environment.NewLine;
        }
    }

    if (int.TryParse(options.Get("budget-lines"), out var lines))
    {
        var split = text.Replace("\r", "", StringComparison.Ordinal).Split('\n');
        if (split.Length > lines)
        {
            text = string.Join(Environment.NewLine, split.Take(lines)) + Environment.NewLine + "[truncated by --budget-lines]" + Environment.NewLine;
        }
    }

    return text;
}
