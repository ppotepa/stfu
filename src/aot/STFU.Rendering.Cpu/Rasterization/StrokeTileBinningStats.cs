namespace STFU.Rendering.Cpu.Rasterization;

public readonly record struct StrokeTileBinningStats(
    int Segments,
    int TileCount,
    int SegmentTileRefs,
    int EmptyTiles,
    int MaxSegmentsPerTile,
    int WorkerCount)
{
    public static StrokeTileBinningStats Empty(int workerCount)
    {
        return new StrokeTileBinningStats(0, 0, 0, 0, 0, workerCount);
    }

    public string ToDiagnosticString()
    {
        return $"segments={Segments}, tiles={TileCount}, refs={SegmentTileRefs}, emptyTiles={EmptyTiles}, maxSegmentsPerTile={MaxSegmentsPerTile}, workers={WorkerCount}";
    }
}
