using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class RunCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("run", root, Path.GetRelativePath(root, solution));
        var command = options.Get("command") ?? options.Get("arg0");
        if (string.IsNullOrWhiteSpace(command))
        {
            return AgentEnvelope.Failure("run", root, Path.GetRelativePath(root, solution), "missing_command", "Provide --command <command line>.");
        }

        var runId = $"{DateTimeOffset.Now:yyyy-MM-dd}/{DateTimeOffset.Now:HHmmssfff}";
        var runDir = Path.Combine(root, ".logs", "agent", runId.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(runDir);
        var result = await ProcessRunner.RunAsync("pwsh", ["-NoProfile", "-Command", command], options.Get("cwd") is { Length: > 0 } cwd ? Path.GetFullPath(Path.Combine(root, cwd)) : root);
        File.WriteAllText(Path.Combine(runDir, "stdout.txt"), result.Stdout);
        File.WriteAllText(Path.Combine(runDir, "stderr.txt"), result.Stderr);
        File.WriteAllText(Path.Combine(runDir, "run.json"), $$"""
        {"runId":"{{runId}}","commandLine":"{{command.Replace("\"", "\\\"")}}","exitCode":{{result.ExitCode}},"durationMs":{{stopwatch.ElapsedMilliseconds}}}
        """);

        envelope.Items.Add(new Dictionary<string, object?>
        {
            ["itemType"] = "run",
            ["runId"] = runId,
            ["commandLine"] = command,
            ["exitCode"] = result.ExitCode,
            ["durationMs"] = stopwatch.ElapsedMilliseconds,
            ["stdoutPath"] = Path.GetRelativePath(root, Path.Combine(runDir, "stdout.txt")),
            ["stderrPath"] = Path.GetRelativePath(root, Path.Combine(runDir, "stderr.txt"))
        });
        envelope.ToolExitCode = result.ExitCode == 0 ? 0 : 3;
        envelope.Summary.Count = 1;
        envelope.Summary.Returned = 1;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }
}
