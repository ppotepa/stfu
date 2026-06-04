using Avalonia;
using STFU.Viewport;

namespace STFU.UI;

public static class StfuUiHost
{
    internal static StfuUiStartupOptions StartupOptions { get; private set; } = StfuUiStartupOptions.Default;

    [STAThread]
    public static void Run(
        string[] args,
        Action<string>? log = null)
    {
        StfuUiLog.Configure(log);
        StartupOptions = StfuUiStartupOptions.Parse(args, out var avaloniaArgs);
        StfuUiLog.Write("Starting Avalonia desktop lifetime.");
        StfuUiLog.Write("UI event loop is running. Close the window to stop the process.");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(avaloniaArgs);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<StfuAvaloniaApp>()
            .UsePlatformDetect();
    }
}

internal sealed record StfuUiStartupOptions(
    ViewportRenderMode? RenderMode,
    string? PresetId)
{
    public static StfuUiStartupOptions Default { get; } = new(null, null);

    public static StfuUiStartupOptions Parse(string[] args, out string[] avaloniaArgs)
    {
        ViewportRenderMode? renderMode = null;
        string? presetId = null;
        var remaining = new List<string>(args.Length);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (string.Equals(arg, "--mesh", StringComparison.OrdinalIgnoreCase))
            {
                renderMode = ViewportRenderMode.Mesh;
                continue;
            }

            if (string.Equals(arg, "--comic", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--comic-surface", StringComparison.OrdinalIgnoreCase))
            {
                renderMode = ViewportRenderMode.ComicSurface;
                presetId = "comic-surface";
                continue;
            }

            if (string.Equals(arg, "--npr", StringComparison.OrdinalIgnoreCase))
            {
                renderMode = ViewportRenderMode.Npr;
                if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    presetId = args[++index];
                }
                continue;
            }

            if (string.Equals(arg, "--preset", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                presetId = args[++index];
                if (string.Equals(presetId, "comic-surface", StringComparison.OrdinalIgnoreCase))
                {
                    renderMode = ViewportRenderMode.ComicSurface;
                }
                else
                {
                    renderMode ??= ViewportRenderMode.Npr;
                }
                continue;
            }

            if (string.Equals(arg, "--render", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                renderMode = ParseRenderMode(args[++index], renderMode);
                continue;
            }

            remaining.Add(arg);
        }

        avaloniaArgs = remaining.ToArray();
        return new StfuUiStartupOptions(renderMode, presetId);
    }

    private static ViewportRenderMode? ParseRenderMode(string value, ViewportRenderMode? fallback)
    {
        return value.ToLowerInvariant() switch
        {
            "mesh" => ViewportRenderMode.Mesh,
            "npr" or "sketch" => ViewportRenderMode.Npr,
            "comic" or "comic-surface" => ViewportRenderMode.ComicSurface,
            _ => fallback
        };
    }
}
