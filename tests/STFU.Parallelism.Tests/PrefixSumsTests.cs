using STFU.Parallelism;
using Xunit;

namespace STFU.Parallelism.Tests;

public sealed class PrefixSumsTests
{
    [Fact]
    public void ExclusiveFromCounts_BuildsExpectedOffsets()
    {
        var counts = new[] { 2, 0, 3, 1 };
        var offsets = new int[counts.Length];

        var total = PrefixSums.ExclusiveFromCounts(counts, offsets);

        Assert.Equal(6, total);
        Assert.Equal(new[] { 0, 2, 2, 5 }, offsets);
    }

    [Fact]
    public void ExclusiveFromCounts_Empty_ReturnsZero()
    {
        var counts = Array.Empty<int>();
        var offsets = Array.Empty<int>();

        var total = PrefixSums.ExclusiveFromCounts(counts, offsets);

        Assert.Equal(0, total);
    }

    [Fact]
    public void ExclusiveFromFlags_BuildsExpectedOffsets()
    {
        var flags = new byte[] { 0, 1, 1, 0, 1 };
        var offsets = new int[flags.Length];

        var total = PrefixSums.ExclusiveFromFlags(flags, offsets);

        Assert.Equal(3, total);
        Assert.Equal(new[] { 0, 0, 1, 2, 2 }, offsets);
    }

    [Fact]
    public void ExclusiveFromFlags_Empty_ReturnsZero()
    {
        var flags = Array.Empty<byte>();
        var offsets = Array.Empty<int>();

        var total = PrefixSums.ExclusiveFromFlags(flags, offsets);

        Assert.Equal(0, total);
    }

    [Fact]
    public void ExclusiveFromCounts_ThrowsWhenOffsetsSpanIsTooSmall()
    {
        var counts = new[] { 1, 2 };
        var offsets = new int[1];

        Assert.Throws<ArgumentException>(() => PrefixSums.ExclusiveFromCounts(counts, offsets));
    }

    [Fact]
    public void ExclusiveFromFlags_ThrowsWhenOffsetsSpanIsTooSmall()
    {
        var flags = new byte[] { 1, 0 };
        var offsets = new int[1];

        Assert.Throws<ArgumentException>(() => PrefixSums.ExclusiveFromFlags(flags, offsets));
    }

    [Fact]
    public void ExclusiveFromCounts_Overflow_Throws()
    {
        var counts = new[] { int.MaxValue, 1 };
        var offsets = new int[counts.Length];

        Assert.Throws<OverflowException>(() => PrefixSums.ExclusiveFromCounts(counts, offsets));
    }
}
