namespace STFU.NPR.Composition;

public sealed record NprPresetMetadata(
    string Id,
    string Name,
    string Description,
    bool IsEditable,
    Version Version,
    Version MinimumEngineVersion,
    string Author,
    IReadOnlyList<string> Tags,
    PresetPackaging Packaging)
{
    public PresetVersion PresetVersion { get; init; } = new(
        Version.Major,
        Version.Minor,
        Version.Build < 0 ? 0 : Version.Build);

    public PresetSchema Schema { get; init; } = PresetSchema.Default;
}
