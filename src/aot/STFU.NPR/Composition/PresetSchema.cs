namespace STFU.NPR.Composition;

public sealed record PresetSchema(
    string SchemaId,
    PresetVersion SchemaVersion,
    IReadOnlyList<string> RequiredSections,
    bool SupportsEditableSettings,
    bool SupportsRuntimePlugins)
{
    public static PresetSchema Default { get; } = new(
        "stfu.npr.preset",
        new PresetVersion(1, 0, 0),
        ["feature", "visibility", "tone", "stroke", "export"],
        SupportsEditableSettings: true,
        SupportsRuntimePlugins: true);
}
