namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveOutputHealthReport(
    InteractiveOutputHealthStatus Status,
    int Score,
    int WarningCount,
    string Summary)
{
    public static InteractiveOutputHealthReport Unknown { get; } = new(
        InteractiveOutputHealthStatus.Unknown,
        0,
        0,
        "interactive output health has not been evaluated");
}
