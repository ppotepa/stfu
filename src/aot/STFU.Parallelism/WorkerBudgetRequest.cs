namespace STFU.Parallelism;

/// <summary>
/// Describes how a worker budget should be resolved.
/// </summary>
public readonly record struct WorkerBudgetRequest
{
    /// <summary>
    /// The preferred budget mode.
    /// </summary>
    public WorkerBudgetMode Mode { get; init; }

    /// <summary>
    /// Explicit worker override. Zero means auto.
    /// </summary>
    public int ExplicitWorkerCount { get; init; }

    /// <summary>
    /// Minimum allowed worker count.
    /// </summary>
    public int MinimumWorkers { get; init; }

    /// <summary>
    /// Maximum allowed worker count. Zero means auto.
    /// </summary>
    public int MaximumWorkers { get; init; }

    public WorkerBudgetRequest(
        WorkerBudgetMode Mode = WorkerBudgetMode.Balanced,
        int ExplicitWorkerCount = 0,
        int MinimumWorkers = 1,
        int MaximumWorkers = 0)
    {
        this.Mode = Mode;
        this.ExplicitWorkerCount = ExplicitWorkerCount;
        this.MinimumWorkers = MinimumWorkers;
        this.MaximumWorkers = MaximumWorkers;
    }
}
