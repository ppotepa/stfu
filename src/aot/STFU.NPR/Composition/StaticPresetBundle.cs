namespace STFU.NPR.Composition;

public sealed class StaticPresetBundle : IPresetProvider
{
    private readonly IReadOnlyList<INprPreset> _presets;

    public string ProviderId { get; }

    public StaticPresetBundle(string providerId, IReadOnlyList<INprPreset> presets)
    {
        ProviderId = providerId;
        _presets = presets;
    }

    public IReadOnlyList<INprPreset> GetPresets()
    {
        return _presets;
    }
}
