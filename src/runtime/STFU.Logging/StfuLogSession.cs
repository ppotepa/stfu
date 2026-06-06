using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using STFU.Common.Math;

namespace STFU.Logging;

public sealed class StfuLogSession
{
    private const int DefaultMaxRetainedRuns = 50;

    private StfuLogSession(
        string rootDirectory,
        string dateDirectory,
        int runNumber,
        DateTimeOffset startedAt)
    {
        RootDirectory = rootDirectory;
        DateDirectory = dateDirectory;
        RunNumber = runNumber;
        StartedAt = startedAt;
        RunDirectory = Path.Combine(dateDirectory, runNumber.ToString("000000", CultureInfo.InvariantCulture));
        FileTimestamp = startedAt.ToLocalTime().ToString("HH-mm-ss", CultureInfo.InvariantCulture);
        DisplayFileTimestamp = startedAt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public string RootDirectory { get; }

    public string DateDirectory { get; }

    public string RunDirectory { get; }

    public int RunNumber { get; }

    public DateTimeOffset StartedAt { get; }

    public string FileTimestamp { get; }

    public string DisplayFileTimestamp { get; }

    public static StfuLogSession Start(string? rootDirectory = null, int maxRetainedRuns = DefaultMaxRetainedRuns)
    {
        var startedAt = DateTimeOffset.Now;
        var root = Path.GetFullPath(rootDirectory ?? Path.Combine(Environment.CurrentDirectory, "logs"));
        var dateStamp = startedAt.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dateDirectory = Path.Combine(root, dateStamp);
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(dateDirectory);

        var maxRun = 0;
        foreach (var directory in Directory.EnumerateDirectories(dateDirectory))
        {
            var name = Path.GetFileName(directory);
            if (int.TryParse(name, out var runNumber))
            {
                maxRun = NumericMath.AtLeast(maxRun, runNumber);
            }
        }

        var nextRun = maxRun + 1;
        StfuLogSession? session = null;
        for (var attempt = 0; attempt < 1024; attempt++)
        {
            var candidate = new StfuLogSession(root, dateDirectory, nextRun + attempt, startedAt);
            Directory.CreateDirectory(candidate.RunDirectory);

            var claimPath = Path.Combine(candidate.RunDirectory, ".session.claim");
            try
            {
                using var claim = new FileStream(claimPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(claim);
                writer.WriteLine(startedAt.ToString("O", CultureInfo.InvariantCulture));
                writer.WriteLine(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                session = candidate;
                break;
            }
            catch (IOException)
            {
                continue;
            }
        }

        if (session is null)
        {
            throw new IOException($"Could not allocate a unique log run directory under '{dateDirectory}'.");
        }

        session.ApplyRetention(maxRetainedRuns);
        return session;
    }

    public void WriteMetadata(IReadOnlyList<string> args)
    {
        var path = Path.Combine(RunDirectory, "run.json");
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true
        });

        writer.WriteStartObject();
        writer.WriteNumber("runNumber", RunNumber);
        writer.WriteString("startedAt", StartedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteString("rootDirectory", RootDirectory);
        writer.WriteString("dateDirectory", DateDirectory);
        writer.WriteString("runDirectory", RunDirectory);
        writer.WriteString("fileTimestamp", FileTimestamp);
        writer.WriteString("displayFileTimestamp", DisplayFileTimestamp);
        writer.WriteString("cwd", Environment.CurrentDirectory);
        writer.WriteString("baseDirectory", AppContext.BaseDirectory);
        writer.WriteNumber("processId", Environment.ProcessId);
        WriteStringOrNull(writer, "processPath", Environment.ProcessPath);
        writer.WriteString("commandLine", Environment.CommandLine);
        writer.WriteStartArray("args");
        foreach (var arg in args)
        {
            writer.WriteStringValue(arg);
        }
        writer.WriteEndArray();
        writer.WriteString("machineName", Environment.MachineName);
        writer.WriteString("userName", Environment.UserName);
        writer.WriteString("os", Environment.OSVersion.ToString());
        writer.WriteString("runtime", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        writer.WriteString("runtimeIdentifier", System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);
        writer.WriteString("architecture", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString());
        writer.WriteNumber("processorCount", Environment.ProcessorCount);
        WriteStringOrNull(writer, "gitCommit", TryReadGitValue("rev-parse HEAD"));
        WriteStringOrNull(writer, "gitBranch", TryReadGitValue("rev-parse --abbrev-ref HEAD"));
        writer.WriteEndObject();
    }

    private static void WriteStringOrNull(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteString(propertyName, value);
    }

    private void ApplyRetention(int maxRetainedRuns)
    {
        if (maxRetainedRuns <= 0)
        {
            return;
        }

        var runs = Directory.EnumerateDirectories(DateDirectory)
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
