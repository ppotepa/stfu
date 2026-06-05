using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agent.Cli.Commands;

namespace Agent.Cli.Workspace;

public static class ServerDaemon
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string PipeName(string root)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(root).ToLowerInvariant())))[..16].ToLowerInvariant();
        return $"stfu-agent-{hash}";
    }

    public static string StatePath(string root, AgentConfig config) => Path.Combine(CacheStore.DirectoryPath(root, config), "server.json");

    public static async Task<int> RunAsync(string root, AgentConfig config, ToolOptions options)
    {
        CacheStore.Ensure(root, config);
        var pipeName = PipeName(root);
        var tracePath = Path.Combine(CacheStore.DirectoryPath(root, config), "server-trace.log");
        var startedAt = DateTimeOffset.Now;
        var projectCount = 0;
        var symbolCount = 0;
        var workspaceStatus = "not-loaded";
        SemanticWorkspace? semanticWorkspace = null;
        IReadOnlyList<Dictionary<string, object?>> symbolIndex = [];
        try
        {
            semanticWorkspace = await SemanticWorkspace.LoadAsync(root, config, options);
            projectCount = semanticWorkspace.Solution.Projects.Count();
            symbolIndex = await SymbolIndexBuilder.BuildAsync(root, semanticWorkspace.Solution);
            symbolCount = symbolIndex.Count;
            CacheStore.WriteJsonLines(root, config, "symbols.jsonl", symbolIndex);
            workspaceStatus = "loaded";
        }
        catch (Exception ex)
        {
            workspaceStatus = "load-failed: " + ex.Message;
        }

        File.WriteAllText(StatePath(root, config), JsonSerializer.Serialize(new
        {
            schema = "agent.server.v1",
            status = "running",
            pid = Environment.ProcessId,
            pipeName,
            startedAt,
            root,
            workspaceStatus,
            projectCount
            ,
            symbolCount
        }, JsonOptions));

        var stop = false;
        while (!stop)
        {
            using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await pipe.WaitForConnectionAsync();
            using var reader = new StreamReader(pipe, Encoding.UTF8);
            await using var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true };
            var command = (await reader.ReadLineAsync()) ?? "status";
            Trace(tracePath, "received: " + (command.Length > 120 ? command[..120] : command));
            if (command.StartsWith('{'))
            {
                var request = JsonSerializer.Deserialize<ServerRequest>(command, JsonOptions) ?? new ServerRequest();
                Trace(tracePath, $"dispatch: {request.Command} {string.Join(' ', request.Args)} symbols={symbolIndex.Count}");
                var requestOptions = ToolOptions.Parse(request.Args, config.DefaultFormat);
                var envelope = await CommandDispatcher.ExecuteAsync(
                    request.Command,
                    root,
                    config,
                    requestOptions,
                    new CommandRuntime { Workspace = semanticWorkspace, SymbolIndex = symbolIndex });
                Trace(tracePath, $"complete: {request.Command} items={envelope.Items.Count} cache={envelope.Summary.Cache}");
                await writer.WriteLineAsync(JsonSerializer.Serialize(envelope, JsonOptions));
                continue;
            }

            if (command.StartsWith("cache ", StringComparison.OrdinalIgnoreCase))
            {
                var bucket = command["cache ".Length..].Trim();
                var items = CacheStore.ReadItems(root, config, bucket, new ToolOptions { Format = "json", Max = 50 });
                await writer.WriteLineAsync(JsonSerializer.Serialize(new
                {
                    schema = "agent.server.response.v1",
                    status = "running",
                    pid = Environment.ProcessId,
                    pipeName,
                    startedAt,
                    uptimeMs = (long)(DateTimeOffset.Now - startedAt).TotalMilliseconds,
                    workspaceStatus,
                    projectCount,
                    bucket,
                    count = items.Count,
                    items
                }, JsonOptions));
                continue;
            }

            if (command.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                stop = true;
            }

            await writer.WriteLineAsync(JsonSerializer.Serialize(new
            {
                schema = "agent.server.response.v1",
                status = stop ? "stopping" : "running",
                pid = Environment.ProcessId,
                pipeName,
                startedAt,
                uptimeMs = (long)(DateTimeOffset.Now - startedAt).TotalMilliseconds,
                workspaceStatus,
                projectCount
                ,
                symbolCount
            }, JsonOptions));
        }

        TryDelete(StatePath(root, config));
        return 0;
    }

    public static async Task<Dictionary<string, object?>> SendAsync(string pipeName, string command, int timeoutMs = 1500)
    {
        var sendTask = SendCoreAsync(pipeName, command, timeoutMs);
        var completed = await Task.WhenAny(sendTask, Task.Delay(timeoutMs));
        if (completed != sendTask)
        {
            throw new TimeoutException($"Timed out waiting for server pipe '{pipeName}'.");
        }

        return await sendTask;
    }

    private static async Task<Dictionary<string, object?>> SendCoreAsync(string pipeName, string command, int timeoutMs)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(cts.Token);
        await using var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true };
        using var reader = new StreamReader(pipe, Encoding.UTF8);
        await writer.WriteLineAsync(command);
        var response = await reader.ReadLineAsync(cts.Token) ?? "{}";
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(response, JsonOptions) ?? new Dictionary<string, object?>();
    }

    public static bool IsProcessAlive(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    public static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static void Trace(string path, string message)
    {
        try
        {
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}
