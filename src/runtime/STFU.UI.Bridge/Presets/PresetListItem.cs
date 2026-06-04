namespace STFU.UI.Bridge.Presets;

public sealed record PresetListItem(
    string Id,
    string Name,
    string Description,
    bool IsEditable,
    string PipelineId,
    string Provider)
{
    public string DisplayName => $"{Id} - {Name}";
}
