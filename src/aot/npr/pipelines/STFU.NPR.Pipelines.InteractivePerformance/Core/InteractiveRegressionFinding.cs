namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveRegressionFinding
{
    public required string Metric { get; init; }
    public InteractiveRegressionSeverity Severity { get; init; }
    public double Baseline { get; init; }
    public double Current { get; init; }
    public double Delta { get; init; }
    public string Message { get; init; } = string.Empty;
}
