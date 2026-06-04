namespace STFU.Strokes.Export;

public sealed record ExportLayer(
    string Name,
    IReadOnlyList<STFU.Strokes.StrokePath2D> Paths);
