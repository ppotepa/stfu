using STFU.Parallelism;
using Xunit;

namespace STFU.Parallelism.Tests;

public sealed class DeterministicParallelCancellationTests
{
    [Fact]
    public void ForRanges_CancelledBeforeStart_ThrowsAndDoesNoWork()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var callCount = 0;

        Assert.Throws<OperationCanceledException>(() =>
            DeterministicParallel.ForRanges(
                0,
                1024,
                4,
                cts.Token,
                (_, _, _, _) => callCount++,
                minItemsPerRange: 32));

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ForRanges_CancelledInsideBody_Throws()
    {
        using var cts = new CancellationTokenSource();
        var callCount = 0;

        Assert.ThrowsAny<OperationCanceledException>(() =>
            DeterministicParallel.ForRanges(
                0,
                2048,
                4,
                cts.Token,
                (start, end, _, token) =>
                {
                    callCount++;
                    token.ThrowIfCancellationRequested();
                    if (start == 0)
                    {
                        cts.Cancel();
                    }

                    for (var i = start; i < end; i++)
                    {
                        token.ThrowIfCancellationRequested();
                    }
                },
                minItemsPerRange: 32));

        Assert.True(callCount >= 1);
    }

    [Fact]
    public void ForRanges_CancellationOverload_PreservesStableRanges()
    {
        using var cts = new CancellationTokenSource();
        var expectedRangeCount = DeterministicParallel.GetRangeCount(300, 4, 16);
        var observed = new ParallelRange[expectedRangeCount];

        DeterministicParallel.ForRanges(
            10,
            310,
            4,
            cts.Token,
            (start, end, rangeIndex, token) =>
            {
                token.ThrowIfCancellationRequested();
                observed[rangeIndex] = new ParallelRange(rangeIndex, start, end);
            },
            minItemsPerRange: 16);

        Assert.Equal(expectedRangeCount, observed.Length);
        Assert.Equal(new ParallelRange(0, 10, 85), observed[0]);
        Assert.Equal(new ParallelRange(1, 85, 160), observed[1]);
        Assert.Equal(new ParallelRange(2, 160, 235), observed[2]);
        Assert.Equal(new ParallelRange(3, 235, 310), observed[3]);
    }
}
