namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveStageTimingRecord(
    long FrameId,
    string Stage,
    double Milliseconds)
{
    public bool IsOverBudget(double stageBudgetMs) => Milliseconds > stageBudgetMs;
}
