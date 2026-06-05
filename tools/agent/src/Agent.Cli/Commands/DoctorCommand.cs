using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class DoctorCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("doctor", root, Path.GetRelativePath(root, solution));

        AddCheck(envelope, "repo-root", Directory.Exists(root), root);
        AddCheck(envelope, "agent-config", File.Exists(Path.Combine(root, "tools", "agent", "agent.config.json")), "tools/agent/agent.config.json");
        AddCheck(envelope, "solution", File.Exists(solution), Path.GetRelativePath(root, solution));

        var dotnet = await ProcessRunner.RunAsync("dotnet", ["--version"], root);
        AddCheck(envelope, "dotnet", dotnet.ExitCode == 0, dotnet.Stdout.Trim());

        var rg = await ProcessRunner.RunAsync("rg", ["--version"], root);
        AddCheck(envelope, "rg", rg.ExitCode == 0, rg.ExitCode == 0 ? TextLines.Split(rg.Stdout).FirstOrDefault() ?? "available" : "not found");

        var project = Path.Combine(root, "tools", "agent", "src", "Agent.Cli", "Agent.Cli.csproj");
        AddCheck(envelope, "agent-cli-project", File.Exists(project), Path.GetRelativePath(root, project));
        CacheStore.Ensure(root, config);
        AddCheck(envelope, "cache", Directory.Exists(CacheStore.DirectoryPath(root, config)), config.CacheDirectory);

        var projects = ProjectScanner.FindProjects(root, solution);
        AddCheck(envelope, "projects", projects.Count > 0, $"{projects.Count} project(s)");

        envelope.Summary.Count = envelope.Items.Count;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        envelope.Next.Add(@".\tools\agent\agent.ps1 context --format json");
        envelope.Next.Add(@".\tools\agent\agent.ps1 projects --format json");
        return envelope;
    }

    private static void AddCheck(AgentEnvelope envelope, string name, bool ok, string message)
    {
        envelope.Items.Add(new Dictionary<string, object?>
        {
            ["itemType"] = "check",
            ["name"] = name,
            ["status"] = ok ? "ok" : "failed",
            ["message"] = message
        });

        if (!ok)
        {
            envelope.Warnings.Add($"{name}: {message}");
        }
    }
}
