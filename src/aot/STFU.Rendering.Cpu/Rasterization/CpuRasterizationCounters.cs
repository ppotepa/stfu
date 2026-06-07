namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuRasterizationCounters
{
    public long TileCacheHits;
    public long TileCacheMisses;
    public long StrokeSegmentsInput;
    public long StrokeTileRefs;
    public long StrokeTilesTouched;
    public long StrokePixelTests;
    public long StrokePixelWrites;
    public long TileBinCapacity;
    public long TonePixels;
    public long ToneSourceCoordCacheHits;
    public long ToneSourceCoordCacheMisses;
    public long ToneSameSizeFastPath;
    public long LayerScratchReused;

    public void Reset()
    {
        TileCacheHits = 0;
        TileCacheMisses = 0;
        StrokeSegmentsInput = 0;
        StrokeTileRefs = 0;
        StrokeTilesTouched = 0;
        StrokePixelTests = 0;
        StrokePixelWrites = 0;
        TileBinCapacity = 0;
        TonePixels = 0;
        ToneSourceCoordCacheHits = 0;
        ToneSourceCoordCacheMisses = 0;
        ToneSameSizeFastPath = 0;
        LayerScratchReused = 0;
    }

    public string ToDiagnosticString()
    {
        return $"tiles: cacheHits={TileCacheHits}, cacheMisses={TileCacheMisses}, strokeSegments={StrokeSegmentsInput}, tileRefs={StrokeTileRefs}, tilesTouched={StrokeTilesTouched}, pixelTests={StrokePixelTests}, pixelWrites={StrokePixelWrites}, tileBinCapacity={TileBinCapacity}; tone: pixels={TonePixels}, coordCacheHits={ToneSourceCoordCacheHits}, coordCacheMisses={ToneSourceCoordCacheMisses}, sameSizeFastPath={ToneSameSizeFastPath}, layerScratchReused={LayerScratchReused}";
    }
}
