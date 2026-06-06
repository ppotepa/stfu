namespace STFU.Parallelism;

/// <summary>
/// Describes how a worker budget should be resolved.
/// </summary>
public readonly record struct WorkerBudgetRequest(
    /// <summary>
    /// The preferred budget mode.
    /// </summary>
    WorkerBudgetMode Mode = WorkerBudgetMode.Balanced,
    /// <summary>
    /// Explicit worker override. Zero means auto.
    /// </summary>
    int ExplicitWorkerCount = 0,
    /// <summary>
    /// Minimum allowed worker count.
    /// </summary>
    int MinimumWorkers = 1,
    /// <summary>
    /// Maximum allowed worker count. Zero means auto.
    /// </summary>
    int MaximumWorkers = 0);
