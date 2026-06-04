namespace STFU.NPR.Composition;

public sealed record RuntimePresetPluginManifest(
    string PluginId,
    string DisplayName,
    PresetVersion Version,
    string EntryAssembly,
    string ProviderType,
    IReadOnlyList<string> PresetIds);
