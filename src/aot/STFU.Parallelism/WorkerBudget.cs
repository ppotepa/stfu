using STFU.Common.Math;

namespace STFU.Parallelism;

/// <summary>
/// Resolves a bounded worker count for CPU-parallel work.
/// </summary>
public static class WorkerBudget
{
    /// <summary>
    /// Gets the current logical processor count, clamped to at least one.
    /// </summary>
    public static int LogicalProcessorCount => NumericMath.AtLeast(Environment.ProcessorCount, 1);

    /// <summary>
    /// Resolves the worker count for the current machine.
    /// </summary>
    public static int Resolve(WorkerBudgetRequest request)
    {
        return ResolveForLogicalProcessorCount(request, LogicalProcessorCount);
    }

    /// <summary>
    /// Resolves the worker count for a synthetic logical processor count.
    /// </summary>
    internal static int ResolveForLogicalProcessorCount(
        WorkerBudgetRequest request,
        int logicalProcessorCount)
    {
        var logical = NumericMath.AtLeast(logicalProcessorCount, 1);
        var minimum = NumericMath.AtLeast(request.MinimumWorkers, 1);
        minimum = NumericMath.AtMost(minimum, logical);
        var maximum = request.MaximumWorkers > 0
            ? NumericMath.AtLeast(request.MaximumWorkers, minimum)
            : logical;

        if (maximum < minimum)
        {
            maximum = minimum;
        }

        maximum = NumericMath.AtMost(maximum, logical);

        if (request.Mode == WorkerBudgetMode.SingleThreadDeterministic)
        {
            return 1;
        }

        if (request.ExplicitWorkerCount > 0)
        {
            return NumericMath.Clamp(request.ExplicitWorkerCount, minimum, maximum);
        }

        var resolved = request.Mode switch
        {
            WorkerBudgetMode.BackgroundSafe => ResolveBackgroundSafe(logical),
            WorkerBudgetMode.Balanced => ResolveBalanced(logical),
            WorkerBudgetMode.Performance => ResolvePerformance(logical),
            WorkerBudgetMode.MaxPerformance => logical,
            WorkerBudgetMode.Benchmark => logical,
            _ => ResolveBalanced(logical)
        };

        return NumericMath.Clamp(resolved, minimum, maximum);
    }

    private static int ResolveBalanced(int logical)
    {
        if (logical <= 2)
        {
            return 1;
        }

        if (logical <= 4)
        {
            return 2;
        }

        return NumericMath.AtLeast(logical - 2, 1);
    }

    private static int ResolvePerformance(int logical)
    {
        if (logical <= 1)
        {
            return 1;
        }

        if (logical <= 4)
        {
            return logical - 1;
        }

        return NumericMath.AtLeast(logical - 1, 1);
    }

    private static int ResolveBackgroundSafe(int logical)
    {
        if (logical <= 2)
        {
            return 1;
        }

        if (logical <= 8)
        {
            return NumericMath.AtLeast(logical / 2, 1);
        }

        return NumericMath.AtMost(NumericMath.AtLeast(logical / 2, 1), 4);
    }
}
