namespace STFU.Common.Math;

public enum WorkerBudgetProfile
{
    BackgroundSafe,
    Balanced,
    Performance,
    MaxPerformance,
    Benchmark,
    SingleThreadDeterministic
}

public static class WorkerBudgetMath
{
    public static int Resolve(
        WorkerBudgetProfile profile,
        int logicalProcessorCount,
        int minimumWorkers,
        int maximumWorkers,
        int explicitWorkerCount)
    {
        var logical = NumericMath.AtLeast(logicalProcessorCount, 1);
        var minimum = NumericMath.AtMost(NumericMath.AtLeast(minimumWorkers, 1), logical);
        var maximum = maximumWorkers > 0
            ? NumericMath.AtLeast(maximumWorkers, minimum)
            : logical;

        if (maximum < minimum)
        {
            maximum = minimum;
        }

        maximum = NumericMath.AtMost(maximum, logical);

        if (profile == WorkerBudgetProfile.SingleThreadDeterministic)
        {
            return 1;
        }

        if (explicitWorkerCount > 0)
        {
            return NumericMath.Clamp(explicitWorkerCount, minimum, maximum);
        }

        var resolved = profile switch
        {
            WorkerBudgetProfile.BackgroundSafe => ResolveBackgroundSafe(logical),
            WorkerBudgetProfile.Balanced => ResolveBalanced(logical),
            WorkerBudgetProfile.Performance => ResolvePerformance(logical),
            WorkerBudgetProfile.MaxPerformance => logical,
            WorkerBudgetProfile.Benchmark => logical,
            _ => ResolveBalanced(logical)
        };

        return NumericMath.Clamp(resolved, minimum, maximum);
    }

    public static int ResolveBalanced(int logical)
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

    public static int ResolvePerformance(int logical)
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

    public static int ResolveBackgroundSafe(int logical)
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
