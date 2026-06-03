using Avalonia;

namespace STFU.UI;

public static class StfuUiHost
{
    [STAThread]
    public static void Run(
        string[] args,
        Action<string>? log = null)
    {
        StfuUiLog.Configure(log);
        StfuUiLog.Write("Starting Avalonia desktop lifetime.");
        StfuUiLog.Write("UI event loop is running. Close the window to stop the process.");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<StfuAvaloniaApp>()
            .UsePlatformDetect();
    }
}
