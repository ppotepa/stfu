namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveEvidenceArchiveEntry(
    string RelativePath,
    string Kind,
    string Sha256,
    long Length);
