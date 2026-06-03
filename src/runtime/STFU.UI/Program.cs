using Avalonia;

namespace STFU.UI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<StfuAvaloniaApp>()
            .UsePlatformDetect();
    }
}
