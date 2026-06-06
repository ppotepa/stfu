namespace STFU.Parallelism;

/// <summary>
/// Represents a stable work range. The <see cref="Index"/> is a range index, not a thread id.
/// </summary>
public readonly record struct ParallelRange(
    int Index,
    int StartInclusive,
    int EndExclusive)
{
    /// <summary>
    /// Gets the stable range length, clamped to a non-negative Int32 value.
    /// </summary>
    public int Count
    {
        get
        {
            var length = (long)EndExclusive - StartInclusive;
            if (length <= 0)
            {
                return 0;
            }

            return length > int.MaxValue ? int.MaxValue : (int)length;
        }
    }
}
