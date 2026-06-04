namespace STFU.Strokes.Export;

public sealed record SvgExportOptions(
    SvgExportMode Mode,
    bool IncludeMetadata,
    bool IncludeDebugLayers,
    float Scale,
    string Units,
    IReadOnlyList<string> EnabledLayers)
{
    public static SvgExportOptions Default { get; } = new(
        SvgExportMode.Editable,
        IncludeMetadata: true,
        IncludeDebugLayers: false,
        Scale: 1f,
        Units: "px",
        EnabledLayers: []);
}
