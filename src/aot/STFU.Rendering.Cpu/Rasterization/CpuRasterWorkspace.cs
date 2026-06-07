using STFU.Common.Math;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuRasterWorkspace
{
    public List<CpuStrokeSegment> Segments { get; } = [];

    public List<CpuStrokeSegmentBuilder.PathSortEntry> PathSortScratch { get; } = [];

    public List<int>[] Bins { get; private set; } = [];

    public int BinCount { get; private set; }

    public int[] RangeTileCounts { get; private set; } = [];

    public int[] RangeTileOffsets { get; private set; } = [];

    public int[] TileCounts { get; private set; } = [];

    public int[] TileOffsets { get; private set; } = [];

    public int[] TileWriteCursors { get; private set; } = [];

    public int[] TileSegmentIndices { get; private set; } = [];

    public int[] SequentialTileSegmentIndices { get; private set; } = [];

    public List<CpuStrokeSegment> GridSegments { get; } = [];

    public int[] SourceXMap { get; private set; } = [];

    public int[] SourceYMap { get; private set; } = [];

    public int[] ToneSourceXMap { get; private set; } = [];

    public int[] ToneSourceYMap { get; private set; } = [];

    public byte[] ToneCoverageScratch { get; private set; } = [];

    public byte[] ToneAlphaScratch { get; private set; } = [];

    private readonly Dictionary<TileCacheKey, List<CpuTile>> _tileCache = new();

    public CpuTileLayout TileLayout { get; private set; } = CpuTileLayout.Empty;

    public void ResetForFrame()
    {
        Segments.Clear();
        PathSortScratch.Clear();
        GridSegments.Clear();
    }

    public List<int>[] RentBins(int tileCount)
    {
        if (Bins.Length < tileCount)
        {
            Bins = new List<int>[tileCount];
            for (var i = 0; i < tileCount; i++)
            {
                Bins[i] = [];
            }

            BinCount = tileCount;
            return Bins;
        }

        for (var i = 0; i < BinCount; i++)
        {
            Bins[i].Clear();
        }

        if (BinCount < tileCount)
        {
            for (var i = BinCount; i < tileCount; i++)
            {
                Bins[i] = [];
            }
        }

        BinCount = tileCount;
        return Bins;
    }

    public int[] RentSourceXMap(int width)
    {
        if (SourceXMap.Length < width)
        {
            SourceXMap = new int[width];
        }

        return SourceXMap;
    }

    public int[] RentSourceYMap(int height)
    {
        if (SourceYMap.Length < height)
        {
            SourceYMap = new int[height];
        }

        return SourceYMap;
    }

    public int[] RentToneSourceXMap(int width)
    {
        if (ToneSourceXMap.Length < width)
        {
            ToneSourceXMap = new int[width];
        }

        return ToneSourceXMap;
    }

    public int[] RentToneSourceYMap(int height)
    {
        if (ToneSourceYMap.Length < height)
        {
            ToneSourceYMap = new int[height];
        }

        return ToneSourceYMap;
    }

    public void EnsureToneScratchCapacity(int pixelCount)
    {
        if (ToneCoverageScratch.Length < pixelCount)
        {
            ToneCoverageScratch = new byte[pixelCount];
        }

        if (ToneAlphaScratch.Length < pixelCount)
        {
            ToneAlphaScratch = new byte[pixelCount];
        }
    }

    public CpuTileLayout GetOrCreateTileLayout(int width, int height, int tileSize)
    {
        tileSize = RasterMath.ClampTileSize(tileSize);
        if (TileLayout.Width == width &&
            TileLayout.Height == height &&
            TileLayout.TileSize == tileSize)
        {
            return TileLayout;
        }

        TileLayout = CpuTileLayout.Create(width, height, tileSize);
        return TileLayout;
    }

    public IReadOnlyList<CpuTile> GetTiles(int width, int height, int tileSize)
    {
        var key = new TileCacheKey(width, height, RasterMath.ClampTileSize(tileSize));
        if (_tileCache.TryGetValue(key, out var tiles))
        {
            return tiles;
        }

        tiles = CpuTileScheduler.BuildTilesCore(width, height, key.TileSize);
        _tileCache[key] = tiles;
        return tiles;
    }

    public int[] RentSequentialTileSegmentIndices(int capacity)
    {
        if (SequentialTileSegmentIndices.Length < capacity)
        {
            SequentialTileSegmentIndices = new int[capacity];
        }

        return SequentialTileSegmentIndices;
    }

    public void EnsureTileBinningCapacity(int rangeCount, int tileCount, int totalRefsEstimate)
    {
        var rangeTileLength = NumericMath.AtLeast(rangeCount * tileCount, 0);
        if (RangeTileCounts.Length < rangeTileLength)
        {
            RangeTileCounts = new int[rangeTileLength];
            RangeTileOffsets = new int[rangeTileLength];
        }

        if (TileCounts.Length < tileCount)
        {
            TileCounts = new int[tileCount];
            TileOffsets = new int[tileCount];
        }

        if (TileWriteCursors.Length < rangeTileLength)
        {
            TileWriteCursors = new int[rangeTileLength];
        }

        if (TileSegmentIndices.Length < totalRefsEstimate)
        {
            TileSegmentIndices = new int[totalRefsEstimate];
        }
    }

    private readonly record struct TileCacheKey(int Width, int Height, int TileSize);
}
