using STFU.NPR.Pipeline;
using STFU.NPR.Settings;

namespace STFU.NPR.Composition;

public sealed class ActiveNprPresetState
{
    private readonly NprPresetRegistry _registry;

    public ActiveNprPresetState(NprPresetRegistry registry)
    {
        _registry = registry;
        ApplyPreset(registry.ActivePreset.Metadata.Id);
    }

    public INprPreset ActivePreset { get; private set; } = null!;

    public NprSettings ActiveSettings { get; private set; } = null!;

    public StyleGrammar ActiveGrammar { get; private set; } = null!;

    public INprPipeline ActivePipeline { get; private set; } = null!;

    public void ApplyPreset(string id)
    {
        _registry.SetActive(id);

        var preset = _registry.ActivePreset;
        ActivePreset = preset;
        ActiveSettings = preset.CreateSettings();
        ActiveGrammar = preset.CreateGrammar();
        ActivePipeline = preset.CreatePipeline();
    }
}
