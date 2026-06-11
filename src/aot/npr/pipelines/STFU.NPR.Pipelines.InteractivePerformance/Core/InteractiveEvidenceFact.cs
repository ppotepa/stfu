namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveEvidenceFact(
    string Name,
    string Value,
    InteractiveEvidenceKind Kind = InteractiveEvidenceKind.Unknown,
    InteractiveEvidenceSeverity Severity = InteractiveEvidenceSeverity.Info)
{
    public bool IsFailure => Severity == InteractiveEvidenceSeverity.Failure;
    public bool IsWarning => Severity == InteractiveEvidenceSeverity.Warning;
}
