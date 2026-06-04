using System.Collections.ObjectModel;

namespace STFU.Logging;

public sealed class StfuLogEvent
{
    public StfuLogEvent(
        DateTimeOffset timestamp,
        TimeSpan elapsed,
        StfuLogLevel level,
        int managedThreadId,
        string domain,
        string name,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null,
        Exception? exception = null)
    {
        Timestamp = timestamp;
        Elapsed = elapsed;
        Level = level;
        ManagedThreadId = managedThreadId;
        Domain = domain;
        Name = name;
        Message = message;
        Properties = properties is null
            ? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>())
            : properties;
        Exception = exception;
    }

    public DateTimeOffset Timestamp { get; }

    public TimeSpan Elapsed { get; }

    public StfuLogLevel Level { get; }

    public int ManagedThreadId { get; }

    public string Domain { get; }

    public string Name { get; }

    public string Message { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public Exception? Exception { get; }
}
