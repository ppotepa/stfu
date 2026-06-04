using STFU.Logging;

namespace STFU.UI;

internal static class StfuUiLog
{
    private static Action<string>? _write;

    public static void Configure(Action<string>? write)
    {
        _write = write;
    }

    public static void Write(string message)
    {
        if (_write is not null)
        {
            _write.Invoke(message);
            return;
        }

        StfuLog.Write(StfuLogDomain.Ui, message);
    }
}
