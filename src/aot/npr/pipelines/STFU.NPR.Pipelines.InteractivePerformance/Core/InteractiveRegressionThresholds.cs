namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveRegressionThresholds
{
    public double WarningFrameMsDelta { get; init; } = 2.0;
    public double ErrorFrameMsDelta { get; init; } = 5.0;
    public double WarningFallbackRatioDelta { get; init; } = 0.10;
    public double ErrorFallbackRatioDelta { get; init; } = 0.25;
    public double WarningHealthScoreDrop { get; init; } = 8.0;
    public double ErrorHealthScoreDrop { get; init; } = 16.0;
}
