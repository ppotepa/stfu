using System.Globalization;
using System.Text;

namespace STFU.Logging;

internal sealed class StfuLogWriter : IDisposable
{
    private readonly StfuLogSession _session;
    private readonly object _gate = new();
    private readonly Dictionary<string, StreamWriter> _writers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public StfuLogWriter(StfuLogSession session)
    {
        _session = session;
    }

    public void Write(StfuLogEvent logEvent)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            WriteCore(logEvent);

            if (logEvent.Level >= StfuLogLevel.Error &&
                !string.Equals(logEvent.Domain, StfuLogDomain.Errors, StringComparison.OrdinalIgnoreCase))
            {
                WriteCore(new StfuLogEvent(
                    logEvent.Timestamp,
                    logEvent.Elapsed,
                    logEvent.Level,
                    logEvent.ManagedThreadId,
                    StfuLogDomain.Errors,
                    logEvent.Name,
                    logEvent.Message,
                    logEvent.Properties,
                    logEvent.Exception));
            }
        }
    }

    private void WriteCore(StfuLogEvent logEvent)
    {
        var writer = GetWriter(logEvent.Timestamp, SanitizeDomain(logEvent.Domain));
        writer.WriteLine(FormatLine(logEvent));
        writer.Flush();
    }

    private StreamWriter GetWriter(DateTimeOffset timestamp, string domain)
    {
        var hour = timestamp.ToLocalTime().ToString("HH", CultureInfo.InvariantCulture);
        var key = $"{hour}.{domain}";
        if (_writers.TryGetValue(key, out var writer))
        {
            return writer;
        }

        var path = Path.Combine(_session.RunDirectory, $"{hour}.{domain}.log");
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        _writers[key] = writer;
        return writer;
    }

    private static string FormatLine(StfuLogEvent logEvent)
    {
        var builder = new StringBuilder(256);
        builder.Append(logEvent.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        builder.Append(" | +");
        builder.Append(logEvent.Elapsed.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));
        builder.Append(" | ");
        builder.Append(LevelCode(logEvent.Level));
        builder.Append(" | T");
        builder.Append(logEvent.ManagedThreadId.ToString(CultureInfo.InvariantCulture));
        builder.Append(" | ");
        builder.Append(logEvent.Domain);
        builder.Append(" | ");
        builder.Append(logEvent.Name);
        builder.Append(" | ");
        builder.Append(Normalize(logEvent.Message));

        foreach (var property in logEvent.Properties.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            builder.Append(" | ");
            builder.Append(property.Key);
            builder.Append('=');
            builder.Append(Normalize(Convert.ToString(property.Value, CultureInfo.InvariantCulture) ?? string.Empty));
        }

        if (logEvent.Exception is not null)
        {
            builder.Append(" | exception=");
            builder.Append(Normalize(logEvent.Exception.ToString()));
        }

        return builder.ToString();
    }

    private static string Normalize(string value)
    {
        return value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static string SanitizeDomain(string domain)
    {
        var builder = new StringBuilder(domain.Length);
        foreach (var ch in domain)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_');
        }

        return builder.Length == 0 ? "general" : builder.ToString();
    }

    private static string LevelCode(StfuLogLevel level)
    {
        return level switch
        {
            StfuLogLevel.Trace => "TRC",
            StfuLogLevel.Debug => "DBG",
            StfuLogLevel.Warning => "WRN",
            StfuLogLevel.Error => "ERR",
            StfuLogLevel.Fatal => "FTL",
            _ => "INF"
        };
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var writer in _writers.Values)
            {
                writer.Dispose();
            }

            _writers.Clear();
        }
    }
}
