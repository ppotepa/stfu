using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class PresetCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var name = options.Get("arg0") ?? options.Get("name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return AgentEnvelope.Failure("preset", root, Path.GetRelativePath(root, solution), "missing_preset", "Provide preset name.");
        }

        var script = Path.Combine(root, "tools", "agent", "presets", "stfu", $"{name}.ps1");
        if (!File.Exists(script))
        {
            return AgentEnvelope.Failure("preset", root, Path.GetRelativePath(root, solution), "preset_not_found", Path.GetRelativePath(root, script));
        }

        var result = await ProcessRunner.RunAsync("pwsh", ["-NoProfile", "-File", script], root);
        var envelope = AgentEnvelope.Create("preset", root, Path.GetRelativePath(root, solution));
        envelope.Items.Add(new Dictionary<string, object?>
        {
            ["itemType"] = "presetRun",
            ["name"] = name,
            ["script"] = Path.GetRelativePath(root, script),
            ["exitCode"] = result.ExitCode,
            ["stdout"] = result.Stdout.Length > 4000 ? result.Stdout[..4000] : result.Stdout,
            ["stderr"] = result.Stderr.Length > 2000 ? result.Stderr[..2000] : result.Stderr
        });
        envelope.ToolExitCode = result.ExitCode == 0 ? 0 : 3;
        envelope.Summary.Count = 1;
        envelope.Summary.Returned = 1;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }
}
