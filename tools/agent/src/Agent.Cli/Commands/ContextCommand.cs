using System.Diagnostics;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class ContextCommand
{
    public static async Task<AgentEnvelope> ExecuteAsync(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        var envelope = AgentEnvelope.Create("context", root, Path.GetRelativePath(root, solution));

        var branch = (await ProcessRunner.RunAsync("git", ["rev-parse", "--abbrev-ref", "HEAD"], root)).Stdout.Trim();
        var head = (await ProcessRunner.RunAsync("git", ["rev-parse", "--short", "HEAD"], root)).Stdout.Trim();
        var status = TextLines.Split((await ProcessRunner.RunAsync("git", ["status", "--short"], root)).Stdout);
        var changed = TextLines.Split((await ProcessRunner.RunAsync("git", ["diff", "--name-only"], root)).Stdout);
        var projects = ProjectScanner.FindProjects(root, solution);

        envelope.Items.Add(new Dictionary<string, object?>
        {
            ["itemType"] = "repo",
            ["name"] = Path.GetFileName(root),
            ["branch"] = branch,
            ["head"] = head,
            ["solution"] = Path.GetRelativePath(root, solution),
            ["projects"] = projects.Count,
            ["changedFiles"] = status.Length
        });

        foreach (var file in changed.Take(options.Max))
        {
            envelope.Items.Add(new Dictionary<string, object?>
            {
                ["itemType"] = "change",
                ["file"] = file,
                ["changeKind"] = "modified"
            });
        }

        envelope.Summary.Count = 1 + changed.Length;
        envelope.Summary.Returned = envelope.Items.Count;
        envelope.Summary.Truncated = changed.Length > options.Max;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        envelope.Next.Add(@".\tools\agent\agent.ps1 diff --format json");
        envelope.Next.Add(@".\tools\agent\agent.ps1 projects --changed-only --format json");
        return envelope;
    }
}
