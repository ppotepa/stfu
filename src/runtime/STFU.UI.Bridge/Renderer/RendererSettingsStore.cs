using System.Text.Json;
using STFU.Logging;

namespace STFU.UI.Bridge.Renderer;

public sealed class RendererSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public RendererSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "STFU",
            "renderer-settings.json");
    }

    public RendererSettingsSnapshot Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                StfuLog.Write(
                    StfuLogDomain.Ui,
                    "renderer.settings.default",
                    _path,
                    StfuLogLevel.Debug);
                return new RendererSettingsSnapshot();
            }

            using var stream = File.OpenRead(_path);
            var snapshot = JsonSerializer.Deserialize<RendererSettingsSnapshot>(stream, JsonOptions) ?? new RendererSettingsSnapshot();
            StfuLog.Write(
                StfuLogDomain.Ui,
                "renderer.settings.loaded",
                _path,
                StfuLogLevel.Debug);
            return snapshot;
        }
        catch (Exception exception)
        {
            StfuLog.Write(
                StfuLogDomain.Ui,
                "renderer.settings.load_failed",
                exception.Message,
                StfuLogLevel.Warning,
                new Dictionary<string, object?> { ["path"] = _path },
                exception);
            return new RendererSettingsSnapshot();
        }
    }

    public void Save(RendererSettingsSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using var stream = File.Create(_path);
            JsonSerializer.Serialize(stream, snapshot, JsonOptions);
            StfuLog.Write(
                StfuLogDomain.Ui,
                "renderer.settings.saved",
                _path,
                StfuLogLevel.Debug,
                properties: new Dictionary<string, object?>
                {
                    ["backend"] = snapshot.Backend,
                    ["api"] = snapshot.Api,
                    ["presentation"] = snapshot.Presentation,
                    ["hud"] = snapshot.ShowRendererHud,
                    ["gpuTimings"] = snapshot.EnableGpuTimings
                });
        }
        catch (Exception exception)
        {
            StfuLog.Write(
                StfuLogDomain.Ui,
                "renderer.settings.save_failed",
                exception.Message,
                StfuLogLevel.Error,
                new Dictionary<string, object?> { ["path"] = _path },
                exception);
        }
    }
}
