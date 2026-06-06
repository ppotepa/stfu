using STFU.Common.Math;

namespace STFU.Parallelism;

public static class DeterministicParallel
{
    public static void ForRanges(
        int fromInclusive,
        int toExclusive,
        int workerCount,
        Action<int, int, int> body,
        int minItemsPerRange = 512)
    {
        ForRanges(
            fromInclusive,
            toExclusive,
            workerCount,
            CancellationToken.None,
            (start, end, rangeIndex, _) => body(start, end, rangeIndex),
            minItemsPerRange);
    }

    public static void ForRanges(
        int fromInclusive,
        int toExclusive,
        int workerCount,
        CancellationToken cancellationToken,
        Action<int, int, int, CancellationToken> body,
        int minItemsPerRange = 512)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (toExclusive <= fromInclusive)
        {
            return;
        }

        var count64 = (long)toExclusive - fromInclusive;
        if (count64 > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(toExclusive),
                "The range length must fit in Int32.");
        }

        var count = (int)count64;
        var rangeCount = GetRangeCount(count, workerCount, minItemsPerRange);
        if (rangeCount <= 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
            body(fromInclusive, toExclusive, 0, cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            Parallel.For(
                0,
                rangeCount,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = rangeCount,
                    CancellationToken = cancellationToken
                },
                rangeIndex =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var range = GetRange(fromInclusive, count, rangeCount, rangeIndex);
                    body(range.StartInclusive, range.EndExclusive, range.Index, cancellationToken);
                });
        }
        catch (AggregateException ex) when (ContainsCancellation(ex) || cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    public static int GetRangeCount(
        int totalCount,
        int workerCount,
        int minItemsPerRange = 512)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        minItemsPerRange = NumericMath.AtLeast(minItemsPerRange, 1);
        if (workerCount <= 1 || totalCount < minItemsPerRange)
        {
            return 1;
        }

        var rangesByItems = NumericMath.CeilingDivide(totalCount, minItemsPerRange);

        return NumericMath.AtMost(
            NumericMath.AtLeast(workerCount, 1),
            NumericMath.AtLeast(rangesByItems, 1));
    }

    public static ParallelRange GetRange(
        int fromInclusive,
        int totalCount,
        int rangeCount,
        int rangeIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rangeCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rangeIndex);

        if (rangeIndex >= rangeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rangeIndex));
        }

        var baseSize = totalCount / rangeCount;
        var extra = totalCount % rangeCount;
        var offset = (long)rangeIndex * baseSize + NumericMath.AtMost(rangeIndex, extra);
        var size = (long)baseSize + (rangeIndex < extra ? 1 : 0);
        var start = (long)fromInclusive + offset;
        var end = start + size;

        if (start < int.MinValue || start > int.MaxValue || end < int.MinValue || end > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "The resolved range must fit in Int32.");
        }

        return new ParallelRange(
            rangeIndex,
            (int)start,
            (int)end);
    }

    private static bool ContainsCancellation(AggregateException exception)
    {
        foreach (var inner in exception.Flatten().InnerExceptions)
        {
            if (inner is OperationCanceledException)
            {
                return true;
            }
        }

        return false;
    }
}
