namespace STFU.NPR.Composition;

public sealed class NprPresetRegistry
{
    private readonly Dictionary<string, INprPreset> _presets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IPresetProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<INprPreset> Presets => _presets.Values;
    public IReadOnlyCollection<IPresetProvider> Providers => _providers.Values;

    public INprPreset ActivePreset { get; private set; }

    public NprPresetRegistry(INprPreset activePreset)
    {
        Register(activePreset);
        ActivePreset = activePreset;
    }

    public void Register(INprPreset preset)
    {
        _presets[preset.Metadata.Id] = preset;
    }

    public void Register(IPresetProvider provider)
    {
        _providers[provider.ProviderId] = provider;

        foreach (var preset in provider.GetPresets())
        {
            Register(preset);
        }
    }

    public bool TryGet(string id, out INprPreset preset)
    {
        return _presets.TryGetValue(id, out preset!);
    }

    public void SetActive(string id)
    {
        if (!TryGet(id, out var preset))
        {
            throw new InvalidOperationException($"NPR preset is not registered: {id}");
        }

        ActivePreset = preset;
    }
}
