namespace STFU.NPR.Composition;

public interface IPresetProvider
{
    string ProviderId { get; }

    IReadOnlyList<INprPreset> GetPresets();
}
