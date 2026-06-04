using Avalonia;
using STFU.UI.Bridge.Renderer;
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
    string? PresetId,
    RendererBackendPreference? BackendPreference,
    RendererApiPreference? ApiPreference,
    RendererPresentationPreference? PresentationPreference)
{
    public static StfuUiStartupOptions Default { get; } = new(null, null, null, null, null);

    public static StfuUiStartupOptions Parse(string[] args, out string[] avaloniaArgs)
    {
        ViewportRenderMode? renderMode = null;
        string? presetId = null;
        RendererBackendPreference? backendPreference = null;
        RendererApiPreference? apiPreference = null;
        RendererPresentationPreference? presentationPreference = null;
        var remaining = new List<string>(args.Length);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (string.Equals(arg, "--default", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--line-art", StringComparison.OrdinalIgnoreCase))
            {
                renderMode = ViewportRenderMode.Npr;
                presetId = "default";
                continue;
            }

            if (string.Equals(arg, "--mesh", StringComparison.OrdinalIgnoreCase))
            {
                renderMode = ViewportRenderMode.Mesh;
                continue;
            }

            if (string.Equals(arg, "--cpu", StringComparison.OrdinalIgnoreCase))
            {
                backendPreference = RendererBackendPreference.FullCpu;
                continue;
            }

            if (string.Equals(arg, "--gpu", StringComparison.OrdinalIgnoreCase))
            {
                backendPreference = RendererBackendPreference.CpuDrivenGpu;
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
                else
                {
                    presetId ??= "default";
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

            if (string.Equals(arg, "--backend", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                backendPreference = ParseBackendPreference(args[++index], backendPreference);
                continue;
            }

            if (string.Equals(arg, "--api", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                apiPreference = ParseApiPreference(args[++index], apiPreference);
                continue;
            }

            if (string.Equals(arg, "--present", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                presentationPreference = ParsePresentationPreference(args[++index], presentationPreference);
                continue;
            }

            remaining.Add(arg);
        }

        avaloniaArgs = remaining.ToArray();
        return new StfuUiStartupOptions(renderMode, presetId, backendPreference, apiPreference, presentationPreference);
    }

    private static ViewportRenderMode? ParseRenderMode(string value, ViewportRenderMode? fallback)
    {
        return value.ToLowerInvariant() switch
        {
            "mesh" => ViewportRenderMode.Mesh,
            "npr" or "sketch" or "default" or "line-art" => ViewportRenderMode.Npr,
            "comic" or "comic-surface" => ViewportRenderMode.ComicSurface,
            _ => fallback
        };
    }

    private static RendererBackendPreference? ParseBackendPreference(string value, RendererBackendPreference? fallback)
    {
        return value.ToLowerInvariant() switch
        {
            "cpu" or "fullcpu" or "full-cpu" => RendererBackendPreference.FullCpu,
            "gpu" or "directx" or "dx11" or "cpu-gpu" => RendererBackendPreference.CpuDrivenGpu,
            "auto" => null,
            _ => fallback
        };
    }

    private static RendererApiPreference? ParseApiPreference(string value, RendererApiPreference? fallback)
    {
        return value.ToLowerInvariant() switch
        {
            "auto" => null,
            "dx11" or "directx" or "directx11" or "d3d11" => RendererApiPreference.DirectX11,
            "vulkan" => RendererApiPreference.Vulkan,
            "opengl" or "gl" => RendererApiPreference.OpenGL,
            "d3d12" or "dx12" or "direct3d12" => RendererApiPreference.Direct3D12,
            _ => fallback
        };
    }

    private static RendererPresentationPreference? ParsePresentationPreference(string value, RendererPresentationPreference? fallback)
    {
        return value.ToLowerInvariant() switch
        {
            "auto" => null,
            "direct" or "gpu" => RendererPresentationPreference.Direct,
            "readback" or "bitmap" => RendererPresentationPreference.Readback,
            _ => fallback
        };
    }
}
