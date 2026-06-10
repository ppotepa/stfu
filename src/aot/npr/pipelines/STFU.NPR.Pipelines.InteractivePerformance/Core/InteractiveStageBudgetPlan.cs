namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveStageBudgetPlan
{
    public required string Scenario { get; init; }
    public double TargetFrameMs { get; init; }
    public double ProjectionBudgetMs { get; init; }
    public double VisibilityBudgetMs { get; init; }
    public double CandidateBudgetMs { get; init; }
    public double StrokeBudgetMs { get; init; }
    public double ToneBudgetMs { get; init; }
    public double GpuBudgetMs { get; init; }
    public double ReserveBudgetMs { get; init; }

    public double PlannedMs => ProjectionBudgetMs + VisibilityBudgetMs + CandidateBudgetMs + StrokeBudgetMs + ToneBudgetMs + GpuBudgetMs + ReserveBudgetMs;
}
