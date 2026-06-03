namespace STFU.NPR.Composition;

public sealed record NprPresetMetadata(
    string Id,
    string Name,
    string Description,
    bool IsEditable);
