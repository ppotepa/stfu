using System.Globalization;

namespace STFU.Logging;

public static class StfuLog
{
    private static readonly object Gate = new();
    private static StfuLogSession? _session;
    private static StfuLogWriter? _writer;
    private static Action<string>? _consoleWriter;
    private static bool _writeConsole = true;

    public static string? RunDirectory => _session?.RunDirectory;

    public static void Configure(
        StfuLogSession session,
        Action<string>? consoleWriter = null,
        bool writeConsole = true)
    {
        lock (Gate)
        {
            _writer?.Dispose();
            _session = session;
            _writer = new StfuLogWriter(session);
            _consoleWriter = consoleWriter ?? Console.WriteLine;
            _writeConsole = writeConsole;
        }
    }

    public static void Write(string domain, string message)
    {
        Write(domain, "message", message);
    }

    public static void Write(
        string domain,
        string name,
        string message,
        StfuLogLevel level = StfuLogLevel.Info,
        IReadOnlyDictionary<string, object?>? properties = null,
        Exception? exception = null)
    {
        StfuLogWriter? writer;
        StfuLogSession? session;
        Action<string>? consoleWriter;
        bool writeConsole;
        lock (Gate)
        {
            EnsureConfigured();
            writer = _writer;
            session = _session;
            consoleWriter = _consoleWriter;
            writeConsole = _writeConsole;
        }

        var timestamp = DateTimeOffset.Now;
        var logEvent = new StfuLogEvent(
            timestamp,
            session is null ? TimeSpan.Zero : timestamp - session.StartedAt,
            level,
            Environment.CurrentManagedThreadId,
            string.IsNullOrWhiteSpace(domain) ? "general" : domain,
            string.IsNullOrWhiteSpace(name) ? "message" : name,
            message,
            properties,
            exception);

        writer?.Write(logEvent);
        if (writeConsole && ShouldWriteConsole(logEvent))
        {
            consoleWriter?.Invoke($"[{timestamp:HH:mm:ss.fff}] {message}");
        }
    }

    public static void Error(string domain, string name, string message, Exception? exception = null)
    {
        Write(domain, name, message, StfuLogLevel.Error, exception: exception);
    }

    public static void Shutdown()
    {
        lock (Gate)
        {
            _writer?.Dispose();
            _writer = null;
            _session = null;
        }
    }

    private static void EnsureConfigured()
    {
        if (_writer is not null)
        {
            return;
        }

        _session = StfuLogSession.Start();
        _writer = new StfuLogWriter(_session);
        _consoleWriter = Console.WriteLine;
        _writeConsole = true;
    }

    private static bool ShouldWriteConsole(StfuLogEvent logEvent)
    {
        if (logEvent.Level >= StfuLogLevel.Warning)
        {
            return true;
        }

        if (string.Equals(logEvent.Domain, StfuLogDomain.Ui, StringComparison.OrdinalIgnoreCase))
        {
            return logEvent.Level >= StfuLogLevel.Info;
        }

        return string.Equals(logEvent.Domain, StfuLogDomain.Host, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(logEvent.Domain, StfuLogDomain.Errors, StringComparison.OrdinalIgnoreCase);
    }
}
