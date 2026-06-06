using STFU.Common.Math;
using STFU.Parallelism;

namespace STFU.Rendering.Cpu.Rasterization;

public static class CpuTileScheduler
{
    public static IReadOnlyList<CpuTile> BuildTiles(int width, int height, int tileSize)
    {
        return BuildTilesCore(width, height, tileSize);
    }

    internal static List<CpuTile> BuildTilesCore(int width, int height, int tileSize)
    {
        tileSize = RasterMath.ClampTileSize(tileSize);
        var tiles = new List<CpuTile>();
        for (var y = 0; y < height; y += tileSize)
        {
            for (var x = 0; x < width; x += tileSize)
            {
                var bounds = RasterMath.TileBounds(tiles.Count, RasterMath.TilesPerAxis(width, tileSize), tileSize, width, height);
                tiles.Add(new CpuTile(bounds.X, bounds.Y, bounds.Width, bounds.Height));
            }
        }

        return tiles;
    }

    public static void ForEachTile(
        int width,
        int height,
        int tileSize,
        int workerCount,
        bool parallel,
        CpuRasterWorkspace workspace,
        Action<CpuTile> action)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(action);

        var tiles = workspace.GetTiles(width, height, tileSize);
        ForEachTile(tiles, workerCount, parallel, action);
    }

    public static void ForEachTile(
        int width,
        int height,
        int tileSize,
        int workerCount,
        bool parallel,
        Action<CpuTile> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var tiles = BuildTiles(width, height, tileSize);
        ForEachTile(tiles, workerCount, parallel, action);
    }

    private static void ForEachTile(
        IReadOnlyList<CpuTile> tiles,
        int workerCount,
        bool parallel,
        Action<CpuTile> action)
    {
        if (!parallel || workerCount <= 1 || tiles.Count <= 1)
        {
            foreach (var tile in tiles)
            {
                action(tile);
            }

            return;
        }

        DeterministicParallel.ForRanges(
            0,
            tiles.Count,
            workerCount,
            (start, end, _) =>
            {
                for (var i = start; i < end; i++)
                {
                    action(tiles[i]);
                }
            },
            minItemsPerRange: 1);
    }
}
