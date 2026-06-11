namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveAcceptanceChecklistItem(
    string Id,
    string Description,
    bool Passed,
    InteractiveEvidenceSeverity Severity = InteractiveEvidenceSeverity.Info);
