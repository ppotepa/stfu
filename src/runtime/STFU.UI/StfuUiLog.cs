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
        _write?.Invoke(message);
    }
}
