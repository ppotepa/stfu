using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public sealed class CommandRuntime
{
    public SemanticWorkspace? Workspace { get; init; }
    public IReadOnlyList<Dictionary<string, object?>>? SymbolIndex { get; init; }
}

public static class CommandDispatcher
{
    public static async Task<AgentEnvelope> ExecuteAsync(
        string command,
        string root,
        AgentConfig config,
        ToolOptions options,
        CommandRuntime? runtime = null)
    {
        return command.ToLowerInvariant() switch
        {
            "doctor" => await DoctorCommand.ExecuteAsync(root, config, options),
            "context" => await ContextCommand.ExecuteAsync(root, config, options),
            "projects" => ProjectsCommand.Execute(root, config, options),
            "packages" => PackagesCommand.Execute(root, config, options),
            "diff" => await DiffCommand.ExecuteAsync(root, config, options),
            "search" => await SearchCommand.ExecuteAsync(root, config, options),
            "build" => await BuildCommand.ExecuteAsync(root, config, options),
            "test" => await TestCommand.ExecuteAsync(root, config, options),
            "run" => await RunCommand.ExecuteAsync(root, config, options),
            "symbols" => await SymbolsCommand.ExecuteAsync(root, config, options, runtime?.Workspace, runtime?.SymbolIndex),
            "refs" => await RefsCommand.ExecuteAsync(root, config, options, runtime?.Workspace),
            "member" => await MemberCommand.ExecuteAsync(root, config, options, runtime?.Workspace),
            "graph" => await GraphCommand.ExecuteAsync(root, config, options, runtime?.Workspace),
            "hotspots" => HotspotsCommand.Execute(root, config, options),
            "logs" => LogsCommand.Execute(root, config, options),
            "bench" => await BenchCommand.ExecuteAsync(root, config, options),
            "bench-history" => BenchHistoryCommand.Execute(root, config, options),
            "concat" or "concat-query" => ConcatCommand.Execute(root, config, options),
            "preset" => await PresetCommand.ExecuteAsync(root, config, options),
            "server" => ServerCommand.Execute(root, config, options),
            "cache" => CacheCommand.Execute(root, config, options),
            _ => AgentEnvelope.Failure(command, root, config.DefaultSolution, "unknown_command", $"Unknown command '{command}'.")
        };
    }
}
