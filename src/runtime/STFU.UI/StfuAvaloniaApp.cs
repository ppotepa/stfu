using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace STFU.UI;

public sealed class StfuAvaloniaApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        StfuUiLog.Write("Avalonia styles initialized.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            StfuUiLog.Write("Creating main window.");
            desktop.MainWindow = new MainWindow();
            desktop.Exit += (_, _) => StfuUiLog.Write("Avalonia desktop lifetime exiting.");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
