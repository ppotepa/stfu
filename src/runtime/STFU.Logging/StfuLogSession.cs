using System.Diagnostics;
using System.Text.Json;

namespace STFU.Logging;

public sealed class StfuLogSession
{
    private const int DefaultMaxRetainedRuns = 50;

    private StfuLogSession(string rootDirectory, int runNumber, DateTimeOffset startedAt)
    {
        RootDirectory = rootDirectory;
        RunNumber = runNumber;
        StartedAt = startedAt;
        RunDirectory = Path.Combine(rootDirectory, runNumber.ToString("000000"));
    }

    public string RootDirectory { get; }

    public string RunDirectory { get; }

    public int RunNumber { get; }

    public DateTimeOffset StartedAt { get; }

    public static StfuLogSession Start(string? rootDirectory = null, int maxRetainedRuns = DefaultMaxRetainedRuns)
    {
        var root = Path.GetFullPath(rootDirectory ?? Path.Combine(Environment.CurrentDirectory, "logs", "data"));
        Directory.CreateDirectory(root);

        var maxRun = 0;
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(directory);
            if (int.TryParse(name, out var runNumber))
            {
                maxRun = Math.Max(maxRun, runNumber);
            }
        }

        var nextRun = maxRun + 1;

        var session = new StfuLogSession(root, nextRun, DateTimeOffset.Now);
        Directory.CreateDirectory(session.RunDirectory);
        session.ApplyRetention(maxRetainedRuns);
        return session;
    }

    public void WriteMetadata(IReadOnlyList<string> args)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["runNumber"] = RunNumber,
            ["startedAt"] = StartedAt,
            ["rootDirectory"] = RootDirectory,
            ["runDirectory"] = RunDirectory,
            ["cwd"] = Environment.CurrentDirectory,
            ["baseDirectory"] = AppContext.BaseDirectory,
            ["processId"] = Environment.ProcessId,
            ["processPath"] = Environment.ProcessPath,
            ["commandLine"] = Environment.CommandLine,
            ["args"] = args.ToArray(),
            ["machineName"] = Environment.MachineName,
            ["userName"] = Environment.UserName,
            ["os"] = Environment.OSVersion.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["runtimeIdentifier"] = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            ["processorCount"] = Environment.ProcessorCount,
            ["gitCommit"] = TryReadGitValue("rev-parse HEAD"),
            ["gitBranch"] = TryReadGitValue("rev-parse --abbrev-ref HEAD")
        };

        var path = Path.Combine(RunDirectory, "run.json");
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private void ApplyRetention(int maxRetainedRuns)
    {
        if (maxRetainedRuns <= 0)
        {
            return;
        }

        var runs = Directory.EnumerateDirectories(RootDirectory)
            .Select(path => new
            {
                Path = path,
                Name = Path.GetFileName(path),
                Run = int.TryParse(Path.GetFileName(path), out var run) ? run : -1
            })
            .Where(item => item.Run >= 0)
            .OrderByDescending(item => item.Run)
            .Skip(maxRetainedRuns)
            .ToArray();

        foreach (var run in runs)
        {
            try
            {
                Directory.Delete(run.Path, recursive: true);
            }
            catch
            {
                // Retention is best-effort; active readers should not break startup.
            }
        }
    }

    private static string? TryReadGitValue(string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null || !process.WaitForExit(1000) || process.ExitCode != 0)
            {
                return null;
            }

            return process.StandardOutput.ReadToEnd().Trim();
        }
        catch
        {
            return null;
        }
    }
}
