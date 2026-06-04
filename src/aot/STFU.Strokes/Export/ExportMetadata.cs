namespace STFU.Strokes.Export;

public sealed record ExportMetadata(
    string StyleId,
    string PresetId,
    int FrameId,
    IReadOnlyList<ExportLayer> Layers);
