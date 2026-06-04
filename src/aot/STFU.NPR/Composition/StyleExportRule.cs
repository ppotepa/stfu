using STFU.Strokes.Export;

namespace STFU.NPR.Composition;

public sealed record StyleExportRule(
    SvgExportMode DefaultSvgMode,
    bool IncludeMetadata,
    bool IncludeDebugLayers,
    string Units,
    IReadOnlyList<string> PreferredLayers);
