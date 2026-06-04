using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace STFU.UI.Styling;

public static class UiThemeService
{
    private static bool _isDark;

    public static bool IsDark => _isDark;

    public static void Apply(bool isDark)
    {
        if (isDark)
        {
            ApplyDark();
        }
        else
        {
            ApplyLight();
        }
    }

    public static void ApplyLight()
    {
        _isDark = false;
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        ApplyPalette(new ThemePalette(
            Bg: "#EEF0EC",
            Surface: "#FAFBF8",
            Panel: "#FCFCFA",
            Panel2: "#F2F4F0",
            Input: "#FFFFFF",
            Overlay: "#E6FAFBF8",
            Line: "#C6CBC3",
            LineSoft: "#DDE1D9",
            Text: "#20231F",
            Muted: "#697067",
            Active: "#20231F",
            ActiveText: "#FFFFFF",
            Gold: "#E7B52B",
            GoldSoft: "#FFF8E1",
            GoldBorder: "#B58412",
            GoldText: "#221905",
            Green: "#347257",
            Blue: "#2E657D",
            Amber: "#8A6822",
            Rust: "#8A4E45"));
    }

    public static void ApplyDark()
    {
        _isDark = true;
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        ApplyPalette(new ThemePalette(
            Bg: "#171916",
            Surface: "#1E211D",
            Panel: "#222620",
            Panel2: "#2A2F28",
            Input: "#181B17",
            Overlay: "#D71E211D",
            Line: "#3C4339",
            LineSoft: "#32382F",
            Text: "#E8ECE4",
            Muted: "#A3AA9E",
            Active: "#E8ECE4",
            ActiveText: "#171916",
            Gold: "#D5A72C",
            GoldSoft: "#3A3118",
            GoldBorder: "#B58412",
            GoldText: "#FFF2C2",
            Green: "#73B993",
            Blue: "#7BB2C7",
            Amber: "#D5A72C",
            Rust: "#C7776D"));
    }

    public static void Toggle()
    {
        if (_isDark)
        {
            ApplyLight();
        }
        else
        {
            ApplyDark();
        }
    }

    private static void ApplyPalette(ThemePalette palette)
    {
        var resources = Application.Current!.Resources;
        resources["StfuBgBrush"] = Brush(palette.Bg);
        resources["StfuSurfaceBrush"] = Brush(palette.Surface);
        resources["StfuPanelBrush"] = Brush(palette.Panel);
        resources["StfuPanel2Brush"] = Brush(palette.Panel2);
        resources["StfuInputBrush"] = Brush(palette.Input);
        resources["StfuOverlayBrush"] = Brush(palette.Overlay);
        resources["StfuLineBrush"] = Brush(palette.Line);
        resources["StfuLineSoftBrush"] = Brush(palette.LineSoft);
        resources["StfuTextBrush"] = Brush(palette.Text);
        resources["StfuMutedBrush"] = Brush(palette.Muted);
        resources["StfuActiveBrush"] = Brush(palette.Active);
        resources["StfuActiveTextBrush"] = Brush(palette.ActiveText);
        resources["StfuGoldBrush"] = Brush(palette.Gold);
        resources["StfuGoldSoftBrush"] = Brush(palette.GoldSoft);
        resources["StfuGoldBorderBrush"] = Brush(palette.GoldBorder);
        resources["StfuGoldTextBrush"] = Brush(palette.GoldText);
        resources["StfuGreenBrush"] = Brush(palette.Green);
        resources["StfuBlueBrush"] = Brush(palette.Blue);
        resources["StfuAmberBrush"] = Brush(palette.Amber);
        resources["StfuRustBrush"] = Brush(palette.Rust);
    }

    private static SolidColorBrush Brush(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }

    private sealed record ThemePalette(
        string Bg,
        string Surface,
        string Panel,
        string Panel2,
        string Input,
        string Overlay,
        string Line,
        string LineSoft,
        string Text,
        string Muted,
        string Active,
        string ActiveText,
        string Gold,
        string GoldSoft,
        string GoldBorder,
        string GoldText,
        string Green,
        string Blue,
        string Amber,
        string Rust);
}
