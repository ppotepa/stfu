namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveRegressionReport
{
    public required string Scenario { get; init; }
    public IReadOnlyList<InteractiveRegressionFinding> Findings { get; init; } = [];

    public bool HasErrors => Findings.Any(static finding => finding.Severity == InteractiveRegressionSeverity.Error);
    public bool HasWarnings => Findings.Any(static finding => finding.Severity == InteractiveRegressionSeverity.Warning);
}
