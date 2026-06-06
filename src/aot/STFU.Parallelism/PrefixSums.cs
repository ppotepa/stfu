using STFU.Common.Math;

namespace STFU.Parallelism;

public static class PrefixSums
{
    public static int ExclusiveFromCounts(
        ReadOnlySpan<int> counts,
        Span<int> offsets)
    {
        return ScanMath.ExclusiveFromCounts(counts, offsets);
    }

    public static int ExclusiveFromFlags(
        ReadOnlySpan<byte> flags,
        Span<int> offsets)
    {
        return ScanMath.ExclusiveFromFlags(flags, offsets);
    }
}
