using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Semi.Avalonia;
using STFU.UI.Styling;

namespace STFU.UI;

public sealed class StfuAvaloniaApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new SemiTheme
        {
            Locale = CultureInfo.GetCultureInfo("en-US")
        });
        Styles.Add(new StyleInclude(new Uri("avares://STFU.UI/Styling/"))
        {
            Source = new Uri("avares://STFU.UI/Styling/Theme.axaml")
        });
        UiThemeService.ApplyLight();
        StfuUiLog.Write("Avalonia styles initialized.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            StfuUiLog.Write("Creating main window.");
            desktop.MainWindow = new MainWindow(StfuUiHost.StartupOptions);
            desktop.Exit += (_, _) => StfuUiLog.Write("Avalonia desktop lifetime exiting.");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
