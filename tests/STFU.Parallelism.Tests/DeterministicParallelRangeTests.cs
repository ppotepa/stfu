using STFU.Parallelism;
using Xunit;

namespace STFU.Parallelism.Tests;

public sealed class DeterministicParallelRangeTests
{
    [Fact]
    public void GetRangeCount_EmptyRange_ReturnsZero()
    {
        Assert.Equal(0, DeterministicParallel.GetRangeCount(0, 4, 128));
    }

    [Fact]
    public void GetRangeCount_SmallRange_ReturnsOne()
    {
        Assert.Equal(1, DeterministicParallel.GetRangeCount(31, 4, 32));
    }

    [Fact]
    public void GetRangeCount_RespectsWorkerCount()
    {
        Assert.Equal(4, DeterministicParallel.GetRangeCount(4096, 4, 512));
        Assert.Equal(8, DeterministicParallel.GetRangeCount(4096, 16, 512));
    }

    [Fact]
    public void GetRange_ReturnsStableNonOverlappingRanges()
    {
        var ranges = Enumerable.Range(0, 3)
            .Select(index => DeterministicParallel.GetRange(10, 10, 3, index))
            .ToArray();

        Assert.Equal(new ParallelRange(0, 10, 14), ranges[0]);
        Assert.Equal(new ParallelRange(1, 14, 17), ranges[1]);
        Assert.Equal(new ParallelRange(2, 17, 20), ranges[2]);
        Assert.Equal(4, ranges[0].Count);
        Assert.Equal(3, ranges[1].Count);
        Assert.Equal(3, ranges[2].Count);
    }

    [Fact]
    public void GetRange_CoversEveryItemOnce()
    {
        const int fromInclusive = 17;
        const int toExclusive = 1251;
        const int count = toExclusive - fromInclusive;
        const int workerCount = 8;
        const int minItemsPerRange = 100;

        var expectedRangeCount = DeterministicParallel.GetRangeCount(count, workerCount, minItemsPerRange);
        var hits = new int[count];
        var rangeHits = new int[expectedRangeCount];

        DeterministicParallel.ForRanges(
            fromInclusive,
            toExclusive,
            workerCount,
            (startInclusive, endExclusive, rangeIndex) =>
            {
                Interlocked.Increment(ref rangeHits[rangeIndex]);
                for (var item = startInclusive; item < endExclusive; item++)
                {
                    Interlocked.Increment(ref hits[item - fromInclusive]);
                }
            },
            minItemsPerRange);

        Assert.All(hits, hit => Assert.Equal(1, hit));
        Assert.All(rangeHits, hit => Assert.Equal(1, hit));
    }

    [Fact]
    public void GetRange_InvalidRangeIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DeterministicParallel.GetRange(0, 10, 3, 3));
    }

    [Fact]
    public void ForRanges_EmptyRange_DoesNoWork()
    {
        var callCount = 0;

        DeterministicParallel.ForRanges(
            10,
            10,
            workerCount: 4,
            (_, _, _) => callCount++,
            minItemsPerRange: 1);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ForRanges_CoversEveryItemOnce()
    {
        const int fromInclusive = 31;
        const int toExclusive = 3101;
        const int count = toExclusive - fromInclusive;
        const int workerCount = 7;
        const int minItemsPerRange = 256;

        var expectedRangeCount = DeterministicParallel.GetRangeCount(count, workerCount, minItemsPerRange);
        var expected = Enumerable.Range(0, expectedRangeCount)
            .Select(index => DeterministicParallel.GetRange(fromInclusive, count, expectedRangeCount, index))
            .ToArray();

        var hits = new int[count];
        var actual = new ParallelRange[expectedRangeCount];

        DeterministicParallel.ForRanges(
            fromInclusive,
            toExclusive,
            workerCount,
            (startInclusive, endExclusive, rangeIndex) =>
            {
                actual[rangeIndex] = new ParallelRange(rangeIndex, startInclusive, endExclusive);
                for (var item = startInclusive; item < endExclusive; item++)
                {
                    Interlocked.Increment(ref hits[item - fromInclusive]);
                }
            },
            minItemsPerRange);

        Assert.Equal(expected, actual);
        Assert.All(hits, hit => Assert.Equal(1, hit));
    }

    [Fact]
    public void ForRanges_UsesStableRangeOrder()
    {
        const int fromInclusive = 31;
        const int toExclusive = 3101;
        const int count = toExclusive - fromInclusive;
        const int workerCount = 7;
        const int minItemsPerRange = 256;

        var expectedRangeCount = DeterministicParallel.GetRangeCount(count, workerCount, minItemsPerRange);
        var expected = Enumerable.Range(0, expectedRangeCount)
            .Select(index => DeterministicParallel.GetRange(fromInclusive, count, expectedRangeCount, index))
            .ToArray();

        for (var iteration = 0; iteration < 20; iteration++)
        {
            var actual = new ParallelRange[expectedRangeCount];

            DeterministicParallel.ForRanges(
                fromInclusive,
                toExclusive,
                workerCount,
                (startInclusive, endExclusive, rangeIndex) =>
                {
                    actual[rangeIndex] = new ParallelRange(rangeIndex, startInclusive, endExclusive);
                },
                minItemsPerRange);

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ForRanges_SingleThreadModeUsesOneRange()
    {
        var callCount = 0;

        DeterministicParallel.ForRanges(
            25,
            1025,
            workerCount: 1,
            (startInclusive, endExclusive, rangeIndex) =>
            {
                callCount++;
                Assert.Equal(0, rangeIndex);
                Assert.Equal(25, startInclusive);
                Assert.Equal(1025, endExclusive);
            },
            minItemsPerRange: 1);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ForRanges_RangeIndexMatchesRangeCount()
    {
        const int fromInclusive = 0;
        const int toExclusive = 4096;
        const int workerCount = 8;
        const int minItemsPerRange = 256;

        var count = toExclusive - fromInclusive;
        var rangeCount = DeterministicParallel.GetRangeCount(count, workerCount, minItemsPerRange);
        var seen = new int[rangeCount];

        DeterministicParallel.ForRanges(
            fromInclusive,
            toExclusive,
            workerCount,
            (_, _, rangeIndex) =>
            {
                Interlocked.Increment(ref seen[rangeIndex]);
            },
            minItemsPerRange);

        Assert.All(seen, hit => Assert.Equal(1, hit));
    }

    [Fact]
    public void ForRanges_MinItemsPerRange_ForcesCoarserRanges()
    {
        const int fromInclusive = 0;
        const int toExclusive = 2048;

        var coarseRangeCount = DeterministicParallel.GetRangeCount(2048, 8, 1024);
        var fineRangeCount = DeterministicParallel.GetRangeCount(2048, 8, 128);

        Assert.Equal(2, coarseRangeCount);
        Assert.Equal(8, fineRangeCount);

        var coarseCalls = 0;
        DeterministicParallel.ForRanges(
            fromInclusive,
            toExclusive,
            8,
            (_, _, _) => coarseCalls++,
            1024);

        Assert.Equal(2, coarseCalls);
    }

    [Fact]
    public void ForRanges_WithMultipleRanges_UsesMultipleThreads()
    {
        const int workerCount = 4;
        const int expectedRangeCount = 4;
        using var ready = new CountdownEvent(expectedRangeCount);
        using var release = new ManualResetEventSlim(false);
        var threadIds = new int[expectedRangeCount];

        DeterministicParallel.ForRanges(
            0,
            1024,
            workerCount,
            (startInclusive, _, rangeIndex) =>
            {
                threadIds[rangeIndex] = Environment.CurrentManagedThreadId;
                ready.Signal();

                if (!ready.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("Timed out waiting for parallel ranges to start.");
                }

                release.Set();

                if (!release.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("Timed out waiting for parallel ranges to release.");
                }

                Assert.True(startInclusive >= 0);
            },
            minItemsPerRange: 1);

        Assert.True(threadIds.Distinct().Count() > 1);
    }
}
