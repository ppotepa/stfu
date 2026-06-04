namespace STFU.Strokes.Export;

public sealed record ExportResult(
    bool Success,
    string? Error,
    int PathCount,
    IReadOnlyList<ExportWarning> Warnings);
