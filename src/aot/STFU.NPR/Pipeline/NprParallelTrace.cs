using System.Diagnostics;
using STFU.NPR.Debug;
using STFU.Parallelism;

namespace STFU.NPR.Pipeline;

internal static class NprParallelTrace
{
    public static void ForRanges(
        NprContext context,
        string stepName,
        int fromInclusive,
        int toExclusive,
        Action<int, int, int, CancellationToken> body,
        int minItemsPerRange = 512)
    {
        if (!context.EnableRangeTimings)
        {
            DeterministicParallel.ForRanges(
                fromInclusive,
                toExclusive,
                context.WorkerCount,
                context.CancellationToken,
                body,
                minItemsPerRange);
            return;
        }

        DeterministicParallel.ForRanges(
            fromInclusive,
            toExclusive,
            context.WorkerCount,
            context.CancellationToken,
            (start, end, rangeIndex, cancellationToken) =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    body(start, end, rangeIndex, cancellationToken);
                }
                finally
                {
                    sw.Stop();
                    lock (context.RangeTraces)
                    {
                        context.RangeTraces.Add(new NprRangeTrace(
                            stepName,
                            rangeIndex,
                            start,
                            end,
                            Environment.CurrentManagedThreadId,
                            sw.ElapsedTicks));
                    }
                }
            },
            minItemsPerRange);
    }
}
