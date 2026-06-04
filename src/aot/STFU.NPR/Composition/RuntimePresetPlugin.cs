using System.Text.Json;

namespace STFU.NPR.Composition;

public sealed class RuntimePresetPlugin : IPresetProvider
{
    private readonly IReadOnlyList<INprPreset> _presets;

    public RuntimePresetPluginManifest Manifest { get; }

    public string ProviderId => Manifest.PluginId;

    public RuntimePresetPlugin(RuntimePresetPluginManifest manifest, IReadOnlyList<INprPreset> presets)
    {
        Manifest = manifest;
        _presets = presets;
    }

    public IReadOnlyList<INprPreset> GetPresets()
    {
        return _presets;
    }

    public string ManifestToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions(RuntimePresetPluginManifestJsonContext.Default.Options)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(Manifest, new RuntimePresetPluginManifestJsonContext(options).RuntimePresetPluginManifest);
    }

    public static RuntimePresetPluginManifest ManifestFromJson(string json)
    {
        return JsonSerializer.Deserialize(json, RuntimePresetPluginManifestJsonContext.Default.RuntimePresetPluginManifest) as RuntimePresetPluginManifest
            ?? throw new InvalidOperationException("Runtime preset plugin manifest could not be deserialized.");
    }
}
