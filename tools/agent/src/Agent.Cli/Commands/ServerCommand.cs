using System.Diagnostics;
using System.Text.Json;
using Agent.Cli.Protocol;
using Agent.Cli.Workspace;

namespace Agent.Cli.Commands;

public static class ServerCommand
{
    public static AgentEnvelope Execute(string root, AgentConfig config, ToolOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var solution = ProjectScanner.ResolveSolution(root, config, options);
        CacheStore.Ensure(root, config);
        var statePath = ServerDaemon.StatePath(root, config);
        var action = options.Get("arg0") ?? options.Get("action") ?? "status";
        var envelope = AgentEnvelope.Create("server", root, Path.GetRelativePath(root, solution));

        var state = ReadState(statePath);
        if (action.Equals("start", StringComparison.OrdinalIgnoreCase))
        {
            if (TryReadPid(state, out var pid) && ServerDaemon.IsProcessAlive(pid))
            {
                AddItem(envelope, action, "running", statePath, state);
            }
            else
            {
                ServerDaemon.TryDelete(statePath);
                StartDaemon(root);
                Thread.Sleep(500);
                AddItem(envelope, action, File.Exists(statePath) ? "running" : "starting", statePath, ReadState(statePath));
            }
        }
        else if (action.Equals("stop", StringComparison.OrdinalIgnoreCase))
        {
            var stopped = StopDaemon(state).GetAwaiter().GetResult();
            if (!stopped) ServerDaemon.TryDelete(statePath);
            AddItem(envelope, action, stopped ? "stopped" : "stale-cleared", statePath, ReadState(statePath));
        }
        else
        {
            var status = StatusDaemon(state).GetAwaiter().GetResult();
            AddItem(envelope, action, status.Status, statePath, status.State);
        }

        envelope.Summary.Count = 1;
        envelope.Summary.Returned = 1;
        envelope.Summary.DurationMs = stopwatch.ElapsedMilliseconds;
        return envelope;
    }

    private static void StartDaemon(string root)
    {
        var dll = typeof(ServerCommand).Assembly.Location;
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        start.ArgumentList.Add(dll);
        start.ArgumentList.Add("server-daemon");
        _ = Process.Start(start);
    }

    private static async Task<bool> StopDaemon(Dictionary<string, object?> state)
    {
        if (!TryReadPipe(state, out var pipeName)) return false;
        try
        {
            await ServerDaemon.SendAsync(pipeName, "stop", 2000);
            return true;
        }
        catch
        {
            if (TryReadPid(state, out var pid))
            {
                try
                {
                    Process.GetProcessById(pid).Kill(entireProcessTree: true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }

    private static async Task<(string Status, Dictionary<string, object?> State)> StatusDaemon(Dictionary<string, object?> state)
    {
        if (!TryReadPid(state, out var pid) || !ServerDaemon.IsProcessAlive(pid))
        {
            return ("stopped", state);
        }

        if (!TryReadPipe(state, out var pipeName))
        {
            return ("running-no-pipe", state);
        }

        try
        {
            var response = await ServerDaemon.SendAsync(pipeName, "status");
            return ("running", response);
        }
        catch
        {
            return ("running-unresponsive", state);
        }
    }

    private static Dictionary<string, object?> ReadState(string statePath)
    {
        if (!File.Exists(statePath)) return new Dictionary<string, object?>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(statePath)) ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private static void AddItem(AgentEnvelope envelope, string action, string status, string statePath, Dictionary<string, object?> state)
    {
        var item = new Dictionary<string, object?>
        {
            ["itemType"] = "server",
            ["action"] = action,
            ["status"] = status,
            ["path"] = Path.GetRelativePath(envelope.Root, statePath)
        };
        foreach (var pair in state)
        {
            item[pair.Key] = pair.Value;
        }
        envelope.Items.Add(item);
    }

    private static bool TryReadPid(Dictionary<string, object?> state, out int pid)
    {
        pid = 0;
        return state.TryGetValue("pid", out var value) && int.TryParse(value?.ToString(), out pid);
    }

    private static bool TryReadPipe(Dictionary<string, object?> state, out string pipeName)
    {
        pipeName = "";
        if (!state.TryGetValue("pipeName", out var value) || value == null) return false;
        pipeName = value.ToString() ?? "";
        return pipeName.Length > 0;
    }
}
