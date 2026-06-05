using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class BenchCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var command = options.Get("command") ?? options.Get("args");
        if (string.IsNullOrWhiteSpace(command))
        {
            return AgentEnvelope.Failure("bench", root, config.DefaultSolution, "missing_benchmark", "Provide --command <command line> for phase-1 bench execution.");
        }

        var run = await RunCommand.ExecuteAsync(root, config, ToolOptions.Parse(["--command", command, "--format", "json"], "json"));
        run.Tool = "bench";
        foreach (var item in run.Items)
        {
            item["itemType"] = "benchmarkRun";
            item["benchmark"] = options.Get("name") ?? "custom";
        }
        return run;
    }
}
