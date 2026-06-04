using System.Collections.ObjectModel;
using STFU.Engine;
using STFU.Logging;
using STFU.Messaging.Commands;

namespace STFU.UI.Bridge.Session;

public sealed class UiCommandBus
{
    private const int MaxLogEntries = 160;
    private readonly StfuEngine _engine;

    public UiCommandBus(StfuEngine engine)
    {
        _engine = engine;
    }

    public ObservableCollection<UiCommandLogEntry> Log { get; } = [];

    public event EventHandler? Flushed;

    public int Execute(ICommand command, string? label = null, bool log = true)
    {
        return Execute([command], label ?? command.GetType().Name, log);
    }

    public int Execute(IEnumerable<ICommand> commands, string? label = null, bool log = true)
    {
        var buffer = new CommandBuffer();
        var commandCount = 0;

        foreach (var command in commands)
        {
            buffer.Enqueue(command);
            commandCount++;
        }

        if (commandCount == 0)
        {
            return 0;
        }

        var handled = _engine.Tick(buffer);
        if (log && !string.IsNullOrWhiteSpace(label))
        {
            Record(label, handled);
        }

        Flushed?.Invoke(this, EventArgs.Empty);
        return handled;
    }

    public void Record(string label, int handledCount = 0)
    {
        Log.Insert(0, new UiCommandLogEntry(DateTimeOffset.Now, label, handledCount));
        StfuLog.Write(
            StfuLogDomain.Commands,
            "record",
            label,
            properties: new Dictionary<string, object?>
            {
                ["handled"] = handledCount
            });

        while (Log.Count > MaxLogEntries)
        {
            Log.RemoveAt(Log.Count - 1);
        }
    }
}
