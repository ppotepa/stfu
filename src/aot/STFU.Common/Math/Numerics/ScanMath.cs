namespace STFU.Common.Math;

public static class ScanMath
{
    public static int ExclusiveFromCounts(ReadOnlySpan<int> counts, Span<int> offsets)
    {
        if (offsets.Length < counts.Length)
        {
            throw new ArgumentException("Offsets span is smaller than counts span.", nameof(offsets));
        }

        long total = 0;
        for (var i = 0; i < counts.Length; i++)
        {
            offsets[i] = (int)total;
            total += counts[i];
            if (total > int.MaxValue)
            {
                throw new OverflowException("The prefix sum exceeds Int32.MaxValue.");
            }
        }

        return (int)total;
    }

    public static int ExclusiveFromFlags(ReadOnlySpan<byte> flags, Span<int> offsets)
    {
        if (offsets.Length < flags.Length)
        {
            throw new ArgumentException("Offsets span is smaller than flags span.", nameof(offsets));
        }

        long total = 0;
        for (var i = 0; i < flags.Length; i++)
        {
            offsets[i] = (int)total;
            total += flags[i] != 0 ? 1 : 0;
            if (total > int.MaxValue)
            {
                throw new OverflowException("The prefix sum exceeds Int32.MaxValue.");
            }
        }

        return (int)total;
    }
}
