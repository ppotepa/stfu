namespace STFU.NPR.Analysis;

public sealed class FrameProjectionCacheStats
{
    public int Hits { get; private set; }
    public int Misses { get; private set; }
    public int Evictions { get; private set; }
    public int Entries { get; internal set; }

    public void Hit() => Hits++;

    public void Miss() => Misses++;

    public void Evict() => Evictions++;

    public void Clear()
    {
        Hits = 0;
        Misses = 0;
        Evictions = 0;
        Entries = 0;
    }
}
