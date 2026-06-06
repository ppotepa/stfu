namespace STFU.Parallelism;

/// <summary>
/// Describes the intended worker budget policy.
/// </summary>
public enum WorkerBudgetMode
{
    /// <summary>
    /// Leaves headroom for general UI/runtime work.
    /// </summary>
    Balanced = 0,
    /// <summary>
    /// Uses almost all logical processors while keeping one free.
    /// </summary>
    Performance = 1,
    /// <summary>
    /// Uses all logical processors for throughput-sensitive work.
    /// </summary>
    MaxPerformance = 2,
    /// <summary>
    /// Uses throughput-oriented worker selection for benchmarks.
    /// </summary>
    Benchmark = 3,
    /// <summary>
    /// Uses a conservative count for cache/background work.
    /// </summary>
    BackgroundSafe = 4,
    /// <summary>
    /// Forces single-threaded deterministic execution.
    /// </summary>
    SingleThreadDeterministic = 5
}
