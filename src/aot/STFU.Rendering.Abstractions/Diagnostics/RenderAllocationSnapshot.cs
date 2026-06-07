namespace STFU.Rendering.Abstractions.Diagnostics;

public readonly record struct RenderAllocationSnapshot(
    long TotalAllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections)
{
    public static RenderAllocationSnapshot Capture()
    {
        return new RenderAllocationSnapshot(
            GC.GetTotalAllocatedBytes(precise: true),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
    }

    public RenderAllocationDelta Delta(RenderAllocationSnapshot before, int frames)
    {
        return new RenderAllocationDelta(
            TotalAllocatedBytes - before.TotalAllocatedBytes,
            frames <= 0 ? 0 : (TotalAllocatedBytes - before.TotalAllocatedBytes) / frames,
            Gen0Collections - before.Gen0Collections,
            Gen1Collections - before.Gen1Collections,
            Gen2Collections - before.Gen2Collections);
    }
}

public readonly record struct RenderAllocationDelta(
    long AllocatedBytes,
    long AllocatedBytesPerFrame,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections)
{
    public string ToDiagnosticString()
    {
        return $"allocatedBytes={AllocatedBytes}, allocatedBytesPerFrame={AllocatedBytesPerFrame}, gen0={Gen0Collections}, gen1={Gen1Collections}, gen2={Gen2Collections}";
    }
}
